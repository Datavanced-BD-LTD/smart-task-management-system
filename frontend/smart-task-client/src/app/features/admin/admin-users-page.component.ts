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
import { MatTableModule } from '@angular/material/table';
import { filter, finalize, switchMap } from 'rxjs';
import { ApiError } from '../../core/models/api-response.model';
import { ManagedUserResponse } from '../../core/models/admin-user.model';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AdminUsersService } from '../../core/services/admin-users.service';
import { AuthService } from '../../core/services/auth.service';
import {
  ProjectConfirmationDialogComponent,
  ProjectConfirmationDialogData,
} from '../projects/project-confirmation-dialog.component';
import {
  AdminUserFormDialogComponent,
  AdminUserFormDialogData,
  AdminUserFormResult,
} from './admin-user-form-dialog.component';

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTableModule,
    ReactiveFormsModule,
  ],
  selector: 'app-admin-users-page',
  styleUrl: './admin-users-page.component.scss',
  templateUrl: './admin-users-page.component.html',
})
export class AdminUsersPageComponent {
  private readonly adminUsersService = inject(AdminUsersService);
  private readonly apiErrorService = inject(ApiErrorService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  readonly displayedColumns = ['user', 'email', 'roles', 'status', 'createdAt', 'actions'];
  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly users = signal<Readonly<{ items: readonly ManagedUserResponse[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number }> | null>(null);
  readonly loading = signal(false);
  readonly hasLoaded = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<readonly ApiError[]>([]);
  readonly actionErrorMessage = signal<string | null>(null);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly hasUsers = computed(() => (this.users()?.items.length ?? 0) > 0);

  constructor() {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set([]);

    this.adminUsersService
      .list({
        keyword: this.searchControl.value.trim() || undefined,
        pageNumber: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.hasLoaded.set(true);
          if (response.success && response.data) {
            this.users.set(response.data);
            return;
          }
          this.errorMessage.set(response.message || 'Users could not be loaded.');
        },
        error: (error: unknown) => {
          this.hasLoaded.set(true);
          this.errorMessage.set(this.apiErrorService.getMessage(error));
          this.apiErrors.set(this.apiErrorService.getErrors(error));
        },
      });
  }

  searchUsers(): void {
    this.pageIndex.set(0);
    this.loadUsers();
  }

  clearSearch(): void {
    this.searchControl.setValue('');
    this.searchUsers();
  }

  onSearchSubmit(event: SubmitEvent): void {
    event.preventDefault();
    this.searchUsers();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadUsers();
  }

  openCreateDialog(): void {
    this.openUserDialog({ mode: 'create' });
  }

  openEditDialog(user: ManagedUserResponse): void {
    this.openUserDialog({ user, mode: 'edit' });
  }

  openRoleDialog(user: ManagedUserResponse): void {
    if (!this.canChangeRole(user)) return;
    this.openUserDialog({ user, mode: 'role' });
  }

  canChangeRole(user: ManagedUserResponse): boolean {
    return !this.isAdminUser(user) && user.userId !== this.authService.currentUser()?.userId;
  }

  canDeactivate(user: ManagedUserResponse): boolean {
    return this.canChangeRole(user);
  }

  confirmDeactivate(user: ManagedUserResponse): void {
    if (!this.canDeactivate(user)) return;

    const data: ProjectConfirmationDialogData = {
      title: 'Deactivate user?',
      message: `This will prevent ${user.displayName || user.email} from signing in. Existing project and task history will be preserved.`,
      confirmLabel: 'Deactivate user',
    };

    this.dialog
      .open(ProjectConfirmationDialogComponent, { data, width: 'min(30rem, 94vw)' })
      .afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        switchMap(() => {
          this.actionErrorMessage.set(null);
          this.loading.set(true);
          return this.adminUsersService
            .delete(user.userId)
            .pipe(finalize(() => this.loading.set(false)));
        }),
      )
      .subscribe({
        next: () => this.loadUsers(),
        error: (error: unknown) => this.actionErrorMessage.set(this.apiErrorService.getMessage(error)),
      });
  }

  rolesLabel(user: ManagedUserResponse): string {
    return user.roles
      .map((role) => role === 'ProjectManager' ? 'Project Manager' : role === 'TeamMember' ? 'Team Member' : role)
      .join(', ') || 'No role';
  }

  statusLabel(user: ManagedUserResponse): string {
    return user.isActive ? 'Active' : 'Inactive';
  }

  trackUser(_index: number, user: ManagedUserResponse): string {
    return user.userId;
  }

  private openUserDialog(data: AdminUserFormDialogData): void {
    this.dialog
      .open(AdminUserFormDialogComponent, { data, width: 'min(44rem, 94vw)' })
      .afterClosed()
      .pipe(
        filter((result): result is AdminUserFormResult => Boolean(result)),
        switchMap((result) => {
          this.actionErrorMessage.set(null);
          this.loading.set(true);
          const request = data.mode === 'edit' && data.user
            ? this.adminUsersService.update(data.user.userId, result as Parameters<AdminUsersService['update']>[1])
            : data.mode === 'role' && data.user
              ? this.adminUsersService.updateRole(data.user.userId, result as Parameters<AdminUsersService['updateRole']>[1])
              : this.adminUsersService.create(result as Parameters<AdminUsersService['create']>[0]);
          return request.pipe(finalize(() => this.loading.set(false)));
        }),
      )
      .subscribe({
        next: () => this.loadUsers(),
        error: (error: unknown) => this.actionErrorMessage.set(this.apiErrorService.getMessage(error)),
      });
  }

  private isAdminUser(user: ManagedUserResponse): boolean {
    return user.roles.some((role) => role.toLowerCase() === 'admin');
  }
}
