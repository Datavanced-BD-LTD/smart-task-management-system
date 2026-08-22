import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import {
  AddProjectMemberRequest,
  AvailableProjectMemberResponse,
} from '../../core/models/project.model';
import { ApiErrorService } from '../../core/services/api-error.service';
import { ProjectsService } from './projects.service';

export interface ProjectMemberDialogData {
  readonly projectId: string;
}

@Component({
  imports: [
    MatAutocompleteModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    ReactiveFormsModule,
  ],
  selector: 'app-project-member-dialog',
  template: `
    <h2 mat-dialog-title>Add project member</h2>

    <form (submit)="submit($event)" novalidate>
      <mat-dialog-content class="dialog-content">
        <p>Search active Team Members by name or email.</p>

        <mat-form-field appearance="outline">
          <mat-label>Search team members</mat-label>
          <input
            matInput
            [formControl]="searchControl"
            [matAutocomplete]="memberAutocomplete"
            autocomplete="off"
            aria-label="Search team members"
          />
          <mat-autocomplete
            #memberAutocomplete="matAutocomplete"
            [displayWith]="displayOption"
            (optionSelected)="selectMember($event)"
          >
            @if (loading()) {
              <mat-option disabled>Searching team members...</mat-option>
            }

            @for (member of availableMembers(); track member.userId) {
              <mat-option [value]="member">
                <span class="member-name">{{ memberDisplayName(member) }}</span>
                <span class="member-details">
                  {{ member.email }} - {{ roleLabel(member.role) }}
                </span>
              </mat-option>
            }

            @if (!loading() && hasSearched() && availableMembers().length === 0) {
              <mat-option disabled>No available team members found.</mat-option>
            }
          </mat-autocomplete>
          @if (selectionError()) {
            <mat-error>{{ selectionError() }}</mat-error>
          }
        </mat-form-field>

        @if (loading()) {
          <mat-progress-bar mode="indeterminate" aria-label="Searching team members" />
        }

        @if (selectedMember(); as member) {
          <div class="selected-member" role="status">
            <strong>{{ memberDisplayName(member) }}</strong>
            <span>{{ member.email }} - {{ roleLabel(member.role) }}</span>
          </div>
        }

        @if (errorMessage(); as error) {
          <p class="error-message" role="alert">{{ error }}</p>
        }
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit" [disabled]="loading()">Add member</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }

    .dialog-content {
      display: grid;
      gap: 0.75rem;
      min-width: min(32rem, 78vw);
      padding-top: 0.5rem;
    }

    p {
      color: #5f6368;
      margin: 0;
    }

    mat-form-field {
      width: 100%;
    }

    .member-name,
    .member-details {
      display: block;
    }

    .member-details,
    .selected-member span {
      color: #5f6368;
      font-size: 0.85rem;
    }

    .selected-member {
      background: #f1f4fb;
      border-radius: 0.5rem;
      display: grid;
      gap: 0.25rem;
      padding: 0.75rem;
    }

    .error-message {
      color: #b3261e;
    }

    @media (max-width: 620px) {
      .dialog-content {
        min-width: auto;
      }
    }
  `,
})
export class ProjectMemberDialogComponent {
  private readonly data = inject<ProjectMemberDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ProjectMemberDialogComponent>);
  private readonly projectsService = inject(ProjectsService);
  private readonly apiErrorService = inject(ApiErrorService);
  private readonly destroyRef = inject(DestroyRef);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly availableMembers = signal<readonly AvailableProjectMemberResponse[]>([]);
  readonly selectedMember = signal<AvailableProjectMemberResponse | null>(null);
  readonly loading = signal(false);
  readonly hasSearched = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selectionError = signal<string | null>(null);

  constructor() {
    this.searchControl.valueChanges
      .pipe(
        startWith(''),
        map((value) => value.trim()),
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => {
          this.loading.set(true);
          this.hasSearched.set(true);
          this.errorMessage.set(null);
        }),
        switchMap((keyword) =>
          this.projectsService
            .listAvailableMembers(this.data.projectId, {
              keyword,
              pageNumber: 1,
              pageSize: 20,
            })
            .pipe(
              catchError((error: unknown) => {
                this.errorMessage.set(this.apiErrorService.getMessage(error));
                return of(null);
              }),
            ),
        ),
        tap(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((response) => {
        if (!response) {
          this.availableMembers.set([]);
          return;
        }

        if (response.success && response.data) {
          this.availableMembers.set(response.data.items);
          return;
        }

        this.availableMembers.set([]);
        this.errorMessage.set(response.message || 'Available team members could not be loaded.');
      });
  }

  selectMember(event: MatAutocompleteSelectedEvent): void {
    const member = event.option.value as AvailableProjectMemberResponse;
    this.selectedMember.set(member);
    this.selectionError.set(null);
    this.searchControl.setValue(member.displayName, { emitEvent: false });
  }

  displayOption = (value: AvailableProjectMemberResponse | string | null): string => {
    if (!value) {
      return '';
    }

    return typeof value === 'string' ? value : this.memberDisplayName(value);
  };

  memberDisplayName(member: AvailableProjectMemberResponse): string {
    const displayName = member.displayName?.trim();

    if (displayName) {
      return displayName;
    }

    const name = `${member.firstName} ${member.lastName}`.trim();
    return name || member.email || 'Unknown user';
  }

  roleLabel(role: string | null | undefined): string {
    switch (role?.toLowerCase()) {
      case 'projectmanager':
        return 'Project Manager';
      case 'teammember':
        return 'Team Member';
      case 'admin':
        return 'Admin';
      default:
        return role || 'Unknown role';
    }
  }

  submit(event?: Event): void {
    event?.preventDefault();
    const member = this.selectedMember() ?? this.resolveTypedMember();

    if (!member) {
      this.selectionError.set('Select a team member from the search results.');
      return;
    }

    this.selectedMember.set(member);
    const request: AddProjectMemberRequest = { userId: member.userId };
    this.dialogRef.close(request);
  }

  private resolveTypedMember(): AvailableProjectMemberResponse | null {
    const rawValue = this.searchControl.value;
    const typedValue = typeof rawValue === 'string'
      ? rawValue.trim().toLocaleLowerCase()
      : '';

    if (!typedValue) {
      return null;
    }

    const exactMatch = this.availableMembers().find((member) => {
      const displayName = this.memberDisplayName(member).toLocaleLowerCase();
      const fullName = `${member.firstName} ${member.lastName}`.trim().toLocaleLowerCase();
      const email = member.email.trim().toLocaleLowerCase();

      return [displayName, fullName, email].includes(typedValue);
    });

    return exactMatch ?? (this.availableMembers().length === 1
      ? this.availableMembers()[0] ?? null
      : null);
  }
}
