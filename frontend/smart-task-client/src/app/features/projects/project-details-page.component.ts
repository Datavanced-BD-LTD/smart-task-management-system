import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { filter, finalize, switchMap } from 'rxjs';
import { ApiError } from '../../core/models/api-response.model';
import {
  AddProjectMemberRequest,
  ProjectMemberResponse,
  ProjectResponse,
  UpdateProjectRequest,
} from '../../core/models/project.model';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import {
  ProjectConfirmationDialogComponent,
  ProjectConfirmationDialogData,
} from './project-confirmation-dialog.component';
import { ProjectFormDialogComponent, ProjectFormDialogData } from './project-form-dialog.component';
import { ProjectMemberDialogComponent } from './project-member-dialog.component';
import { ProjectsService } from './projects.service';

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatProgressBarModule,
    MatTableModule,
    RouterLink,
  ],
  selector: 'app-project-details-page',
  styleUrl: './project-details-page.component.scss',
  templateUrl: './project-details-page.component.html',
})
export class ProjectDetailsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly projectsService = inject(ProjectsService);
  private readonly apiErrorService = inject(ApiErrorService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  readonly projectId = this.route.snapshot.paramMap.get('projectId');
  readonly project = signal<ProjectResponse | null>(null);
  readonly members = signal<readonly ProjectMemberResponse[]>([]);
  readonly loading = signal(false);
  readonly membersLoading = signal(false);
  readonly actionLoading = signal(false);
  readonly hasLoaded = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<readonly ApiError[]>([]);
  readonly memberErrorMessage = signal<string | null>(null);
  readonly memberApiErrors = signal<readonly ApiError[]>([]);
  readonly memberColumns = ['member', 'addedAtUtc', 'actions'];
  readonly canManage = computed(() => {
    const project = this.project();

    if (!project) {
      return false;
    }

    return (
      this.hasRole('Admin') ||
      (this.hasRole('ProjectManager') &&
        this.authService.currentUser()?.userId === project.projectManagerId)
    );
  });

  ngOnInit(): void {
    this.loadProject();
  }

  loadProject(): void {
    if (!this.projectId) {
      this.hasLoaded.set(true);
      this.errorMessage.set('The project ID is missing or invalid.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set([]);

    this.projectsService
      .getById(this.projectId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.hasLoaded.set(true);

          if (response.success && response.data) {
            this.project.set(response.data);
            this.loadMembers();
            return;
          }

          this.errorMessage.set(response.message || 'Project could not be loaded.');
        },
        error: (error: unknown) => {
          this.hasLoaded.set(true);
          this.errorMessage.set(this.apiErrorService.getMessage(error));
          this.apiErrors.set(this.apiErrorService.getErrors(error));
        },
      });
  }

  loadMembers(): void {
    if (!this.projectId) {
      return;
    }

    this.membersLoading.set(true);
    this.memberErrorMessage.set(null);
    this.memberApiErrors.set([]);

    this.projectsService
      .listMembers(this.projectId)
      .pipe(finalize(() => this.membersLoading.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.members.set(response.data);
            return;
          }

          this.memberErrorMessage.set(response.message || 'Project members could not be loaded.');
        },
        error: (error: unknown) => {
          this.memberErrorMessage.set(this.apiErrorService.getMessage(error));
          this.memberApiErrors.set(this.apiErrorService.getErrors(error));
        },
      });
  }

  openEditDialog(): void {
    const project = this.project();

    if (!project || !this.canManage()) {
      return;
    }

    const data: ProjectFormDialogData = { project };
    this.dialog
      .open(ProjectFormDialogComponent, { data, width: 'min(42rem, 94vw)' })
      .afterClosed()
      .pipe(
        filter((request): request is UpdateProjectRequest => Boolean(request)),
        switchMap((request) => {
          this.actionLoading.set(true);
          this.errorMessage.set(null);
          return this.projectsService
            .update(project.projectId, request)
            .pipe(finalize(() => this.actionLoading.set(false)));
        }),
      )
      .subscribe({
        next: () => this.loadProject(),
        error: (error: unknown) => this.showProjectError(error),
      });
  }

  confirmDelete(): void {
    const project = this.project();

    if (!project || !this.canManage()) {
      return;
    }

    const data: ProjectConfirmationDialogData = {
      title: 'Delete project?',
      message: `This will remove ${project.name} from the project list. This action cannot be undone.`,
      confirmLabel: 'Delete project',
    };

    this.dialog
      .open(ProjectConfirmationDialogComponent, {
        data,
        width: 'min(30rem, 94vw)',
      })
      .afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        switchMap(() => {
          this.actionLoading.set(true);
          this.errorMessage.set(null);
          return this.projectsService
            .delete(project.projectId)
            .pipe(finalize(() => this.actionLoading.set(false)));
        }),
      )
      .subscribe({
        next: () => void this.router.navigate(['/projects']),
        error: (error: unknown) => this.showProjectError(error),
      });
  }

  openAddMemberDialog(): void {
    if (!this.projectId || !this.canManage()) {
      return;
    }

    this.dialog
      .open(ProjectMemberDialogComponent, { width: 'min(42rem, 94vw)' })
      .afterClosed()
      .pipe(
        filter((request): request is AddProjectMemberRequest => Boolean(request)),
        switchMap((request) => {
          this.actionLoading.set(true);
          this.memberErrorMessage.set(null);
          return this.projectsService
            .addMember(this.projectId!, request)
            .pipe(finalize(() => this.actionLoading.set(false)));
        }),
      )
      .subscribe({
        next: () => this.loadMembers(),
        error: (error: unknown) => this.showMemberError(error),
      });
  }

  confirmRemoveMember(member: ProjectMemberResponse): void {
    if (!this.projectId || !this.canManage()) {
      return;
    }

    const data: ProjectConfirmationDialogData = {
      title: 'Remove project member?',
      message: `Remove ${member.firstName} ${member.lastName} from this project?`,
      confirmLabel: 'Remove member',
    };

    this.dialog
      .open(ProjectConfirmationDialogComponent, {
        data,
        width: 'min(30rem, 94vw)',
      })
      .afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        switchMap(() => {
          this.actionLoading.set(true);
          this.memberErrorMessage.set(null);
          return this.projectsService
            .removeMember(this.projectId!, member.userId)
            .pipe(finalize(() => this.actionLoading.set(false)));
        }),
      )
      .subscribe({
        next: () => this.loadMembers(),
        error: (error: unknown) => this.showMemberError(error),
      });
  }

  trackMember(_index: number, member: ProjectMemberResponse): string {
    return member.userId;
  }

  private hasRole(role: string): boolean {
    return this.authService
      .currentRoles()
      .some((currentRole) => currentRole.toLowerCase() === role.toLowerCase());
  }

  private showProjectError(error: unknown): void {
    this.errorMessage.set(this.apiErrorService.getMessage(error));
    this.apiErrors.set(this.apiErrorService.getErrors(error));
  }

  private showMemberError(error: unknown): void {
    this.memberErrorMessage.set(this.apiErrorService.getMessage(error));
    this.memberApiErrors.set(this.apiErrorService.getErrors(error));
  }
}
