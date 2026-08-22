import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { filter, finalize, switchMap } from 'rxjs';
import { ApiError } from '../../core/models/api-response.model';
import { PagedResponse } from '../../core/models/pagination.model';
import { CreateProjectRequest, ProjectResponse } from '../../core/models/project.model';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import {
  ProjectConfirmationDialogComponent,
  ProjectConfirmationDialogData,
} from './project-confirmation-dialog.component';
import { ProjectFormDialogComponent, ProjectFormDialogData } from './project-form-dialog.component';
import { ProjectsService } from './projects.service';

type ProjectSortColumn = 'name' | 'createdAt' | 'updatedAt';
type ProjectSortDirection = 'asc' | 'desc';

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSortModule,
    MatTableModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  selector: 'app-projects-page',
  styleUrl: './projects-page.component.scss',
  templateUrl: './projects-page.component.html',
})
export class ProjectsPageComponent {
  private readonly projectsService = inject(ProjectsService);
  private readonly apiErrorService = inject(ApiErrorService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  readonly displayedColumns = [
    'name',
    'projectManager',
    'description',
    'createdAtUtc',
    'updatedAtUtc',
    'actions',
  ];
  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly projects = signal<PagedResponse<ProjectResponse> | null>(null);
  readonly loading = signal(false);
  readonly actionLoading = signal(false);
  readonly hasLoaded = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<readonly ApiError[]>([]);
  readonly actionErrorMessage = signal<string | null>(null);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly sortBy = signal<ProjectSortColumn>('createdAt');
  readonly sortDirection = signal<ProjectSortDirection>('desc');
  readonly canCreate = computed(() => this.hasRole('Admin') || this.hasRole('ProjectManager'));

  constructor() {
    this.loadProjects();
  }

  loadProjects(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set([]);

    this.projectsService
      .list({
        search: this.searchControl.value.trim() || undefined,
        sortBy: this.sortBy(),
        sortDirection: this.sortDirection(),
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.hasLoaded.set(true);

          if (response.success && response.data) {
            this.projects.set(response.data);
            return;
          }

          this.errorMessage.set(response.message || 'Projects could not be loaded.');
        },
        error: (error: unknown) => {
          this.hasLoaded.set(true);
          this.errorMessage.set(this.apiErrorService.getMessage(error));
          this.apiErrors.set(this.apiErrorService.getErrors(error));
        },
      });
  }

  searchProjects(): void {
    this.pageIndex.set(0);
    this.loadProjects();
  }

  clearSearch(): void {
    this.searchControl.setValue('');
    this.searchProjects();
  }

  onSearchSubmit(event: SubmitEvent): void {
    event.preventDefault();
    this.searchProjects();
  }

  onSort(sort: Sort): void {
    const supportedColumns: readonly ProjectSortColumn[] = ['name', 'createdAt', 'updatedAt'];
    const nextColumn = supportedColumns.includes(sort.active as ProjectSortColumn)
      ? (sort.active as ProjectSortColumn)
      : 'createdAt';

    this.sortBy.set(nextColumn);
    this.sortDirection.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.pageIndex.set(0);
    this.loadProjects();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadProjects();
  }

  openCreateDialog(): void {
    if (!this.canCreate()) {
      return;
    }

    const data: ProjectFormDialogData = {};
    this.dialog
      .open(ProjectFormDialogComponent, { data, width: 'min(42rem, 94vw)' })
      .afterClosed()
      .pipe(
        filter((request): request is CreateProjectRequest => Boolean(request)),
        switchMap((request) => {
          this.actionLoading.set(true);
          this.actionErrorMessage.set(null);
          return this.projectsService
            .create(request)
            .pipe(finalize(() => this.actionLoading.set(false)));
        }),
      )
      .subscribe({
        next: () => this.loadProjects(),
        error: (error: unknown) => this.showActionError(error),
      });
  }

  openEditDialog(project: ProjectResponse): void {
    if (!this.canManage(project)) {
      return;
    }

    const data: ProjectFormDialogData = { project };
    this.dialog
      .open(ProjectFormDialogComponent, { data, width: 'min(42rem, 94vw)' })
      .afterClosed()
      .pipe(
        filter((request): request is CreateProjectRequest => Boolean(request)),
        switchMap((request) => {
          this.actionLoading.set(true);
          this.actionErrorMessage.set(null);
          return this.projectsService
            .update(project.projectId, request)
            .pipe(finalize(() => this.actionLoading.set(false)));
        }),
      )
      .subscribe({
        next: () => this.loadProjects(),
        error: (error: unknown) => this.showActionError(error),
      });
  }

  confirmDelete(project: ProjectResponse): void {
    if (!this.canManage(project)) {
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
          this.actionErrorMessage.set(null);
          return this.projectsService
            .delete(project.projectId)
            .pipe(finalize(() => this.actionLoading.set(false)));
        }),
      )
      .subscribe({
        next: () => this.loadProjects(),
        error: (error: unknown) => this.showActionError(error),
      });
  }

  canManage(project: ProjectResponse): boolean {
    if (this.hasRole('Admin')) {
      return true;
    }

    return (
      this.hasRole('ProjectManager') &&
      this.authService.currentUser()?.userId === project.projectManagerId
    );
  }

  trackProject(_index: number, project: ProjectResponse): string {
    return project.projectId;
  }

  userLabel(name: string | null | undefined, email: string | null | undefined): string {
    return name?.trim() || email?.trim() || 'Unknown user';
  }

  private hasRole(role: string): boolean {
    return this.authService
      .currentRoles()
      .some((currentRole) => currentRole.toLowerCase() === role.toLowerCase());
  }

  private showActionError(error: unknown): void {
    this.actionErrorMessage.set(this.apiErrorService.getMessage(error));
  }
}
