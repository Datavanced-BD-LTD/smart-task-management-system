import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatNativeDateModule, provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable, filter, finalize, forkJoin, switchMap } from 'rxjs';
import { ApiError } from '../../core/models/api-response.model';
import { PagedResponse } from '../../core/models/pagination.model';
import { ProjectMemberResponse, ProjectResponse } from '../../core/models/project.model';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import {
  AssignTaskRequest,
  CreateTaskRequest,
  TASK_PRIORITY_DEFINITIONS,
  TASK_STATUS_DEFINITIONS,
  TaskListQuery,
  TaskPriority,
  TaskResponse,
  TaskStatus,
  taskPriorityLabel,
  taskStatusLabel,
  UpdateTaskPriorityRequest,
  UpdateTaskStatusRequest,
} from '../../core/models/task.model';
import {
  ProjectConfirmationDialogComponent,
  ProjectConfirmationDialogData,
} from '../projects/project-confirmation-dialog.component';
import { ProjectsService } from '../projects/projects.service';
import {
  TaskAssignmentDialogComponent,
  TaskAssignmentDialogData,
} from './task-assignment-dialog.component';
import { TaskFormDialogComponent, TaskFormDialogData } from './task-form-dialog.component';
import { TasksService } from './tasks.service';

type TaskSortColumn = 'title' | 'status' | 'priority' | 'dueDate' | 'createdAt';
type TaskSortDirection = 'asc' | 'desc';

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatNativeDateModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSortModule,
    MatTableModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  providers: [provideNativeDateAdapter()],
  selector: 'app-tasks-page',
  styleUrl: './tasks-page.component.scss',
  templateUrl: './tasks-page.component.html',
})
export class TasksPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private readonly tasksService = inject(TasksService);
  private readonly projectsService = inject(ProjectsService);
  private readonly apiErrorService = inject(ApiErrorService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  readonly projectId = this.route.snapshot.paramMap.get('projectId');
  readonly project = signal<ProjectResponse | null>(null);
  readonly members = signal<readonly ProjectMemberResponse[]>([]);
  readonly tasks = signal<PagedResponse<TaskResponse> | null>(null);
  readonly contextLoading = signal(false);
  readonly tasksLoading = signal(false);
  readonly actionLoading = signal<string | null>(null);
  readonly hasLoaded = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<readonly ApiError[]>([]);
  readonly memberErrorMessage = signal<string | null>(null);
  readonly actionErrorMessage = signal<string | null>(null);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly sortColumn = signal<TaskSortColumn>('createdAt');
  readonly sortDirection = signal<TaskSortDirection>('desc');
  readonly displayedColumns = [
    'title',
    'description',
    'status',
    'priority',
    'assignedTo',
    'dueDate',
    'createdAtUtc',
    'actions',
  ];
  readonly statuses = TASK_STATUS_DEFINITIONS;
  readonly priorities = TASK_PRIORITY_DEFINITIONS;
  readonly filtersForm = this.formBuilder.group({
    keyword: this.formBuilder.nonNullable.control(''),
    status: this.formBuilder.control<TaskStatus | null>(null),
    priority: this.formBuilder.control<TaskPriority | null>(null),
    assignedUserId: this.formBuilder.nonNullable.control(''),
    dueDateFrom: this.formBuilder.control<Date | null>(null),
    dueDateTo: this.formBuilder.control<Date | null>(null),
  });

  readonly canManageTasks = computed(() => {
    const currentProject = this.project();

    if (!currentProject) {
      return false;
    }

    return (
      this.hasRole('Admin') ||
      (this.hasRole('ProjectManager') &&
        this.authService.currentUser()?.userId === currentProject.projectManagerId)
    );
  });

  ngOnInit(): void {
    this.loadContext();
  }

  loadContext(): void {
    if (!this.projectId) {
      this.hasLoaded.set(true);
      this.errorMessage.set('Select a project to view its tasks.');
      return;
    }

    this.contextLoading.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set([]);
    this.memberErrorMessage.set(null);

    forkJoin({
      project: this.projectsService.getById(this.projectId),
      members: this.projectsService.listMembers(this.projectId),
    })
      .pipe(finalize(() => this.contextLoading.set(false)))
      .subscribe({
        next: ({ project, members }) => {
          if (!project.success || !project.data) {
            this.errorMessage.set(project.message || 'Project could not be loaded.');
            return;
          }

          this.project.set(project.data);

          if (members.success && members.data) {
            this.members.set(members.data);
          } else {
            this.memberErrorMessage.set(members.message || 'Project members could not be loaded.');
          }

          this.loadTasks();
        },
        error: (error: unknown) => {
          this.hasLoaded.set(true);
          this.errorMessage.set(this.apiErrorService.getMessage(error));
          this.apiErrors.set(this.apiErrorService.getErrors(error));
        },
      });
  }

  loadTasks(): void {
    if (!this.projectId) {
      return;
    }

    this.tasksLoading.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set([]);

    this.tasksService
      .list(this.projectId, this.buildQuery())
      .pipe(finalize(() => this.tasksLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.hasLoaded.set(true);

          if (response.success && response.data) {
            this.tasks.set(response.data);
            return;
          }

          this.errorMessage.set(response.message || 'Tasks could not be loaded.');
        },
        error: (error: unknown) => {
          this.hasLoaded.set(true);
          this.errorMessage.set(this.apiErrorService.getMessage(error));
          this.apiErrors.set(this.apiErrorService.getErrors(error));
        },
      });
  }

  applyFilters(): void {
    this.pageIndex.set(0);
    this.loadTasks();
  }

  clearFilters(): void {
    this.filtersForm.reset({
      keyword: '',
      status: null,
      priority: null,
      assignedUserId: '',
      dueDateFrom: null,
      dueDateTo: null,
    });
    this.applyFilters();
  }

  onSort(sort: Sort): void {
    const supportedColumns: readonly TaskSortColumn[] = [
      'title',
      'status',
      'priority',
      'dueDate',
      'createdAt',
    ];
    const nextColumn = supportedColumns.includes(sort.active as TaskSortColumn)
      ? (sort.active as TaskSortColumn)
      : 'createdAt';

    this.sortColumn.set(nextColumn);
    this.sortDirection.set(sort.direction === 'asc' ? 'asc' : 'desc');
    this.pageIndex.set(0);
    this.loadTasks();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(Math.min(event.pageSize, 100));
    this.loadTasks();
  }

  openCreateDialog(): void {
    if (!this.projectId || !this.canManageTasks()) {
      return;
    }

    const data: TaskFormDialogData = { members: this.members() };
    this.dialog
      .open(TaskFormDialogComponent, {
        data,
        width: 'min(42rem, calc(100vw - 2rem))',
        maxWidth: 'calc(100vw - 2rem)',
      })
      .afterClosed()
      .pipe(
        filter((request): request is CreateTaskRequest => Boolean(request)),
        switchMap((request) =>
          this.runAction('create', this.tasksService.create(this.projectId!, request)),
        ),
      )
      .subscribe({
        next: () => this.loadTasks(),
        error: (error: unknown) => this.showActionError(error),
      });
  }

  openEditDialog(task: TaskResponse): void {
    if (!this.canManageTasks()) {
      return;
    }

    const data: TaskFormDialogData = { task, members: this.members() };
    this.dialog
      .open(TaskFormDialogComponent, {
        data,
        width: 'min(42rem, calc(100vw - 2rem))',
        maxWidth: 'calc(100vw - 2rem)',
      })
      .afterClosed()
      .pipe(
        filter((request): request is CreateTaskRequest => Boolean(request)),
        switchMap((request) => this.runAction(task.id, this.tasksService.update(task.id, request))),
      )
      .subscribe({
        next: () => this.loadTasks(),
        error: (error: unknown) => this.showActionError(error),
      });
  }

  confirmDelete(task: TaskResponse): void {
    if (!this.canManageTasks()) {
      return;
    }

    const data: ProjectConfirmationDialogData = {
      title: 'Delete task?',
      message: `This will permanently remove “${task.title}”. This action cannot be undone.`,
      confirmLabel: 'Delete task',
    };

    this.dialog
      .open(ProjectConfirmationDialogComponent, {
        data,
        width: 'min(30rem, 94vw)',
      })
      .afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        switchMap(() => this.runAction(task.id, this.tasksService.delete(task.id))),
      )
      .subscribe({
        next: () => this.loadTasks(),
        error: (error: unknown) => this.showActionError(error),
      });
  }

  openAssignmentDialog(task: TaskResponse): void {
    if (!this.canManageTasks()) {
      return;
    }

    const data: TaskAssignmentDialogData = { task, members: this.members() };
    this.dialog
      .open(TaskAssignmentDialogComponent, { data, width: 'min(36rem, 94vw)' })
      .afterClosed()
      .pipe(
        filter((request): request is AssignTaskRequest => Boolean(request)),
        switchMap((request) => this.runAction(task.id, this.tasksService.assign(task.id, request))),
      )
      .subscribe({
        next: () => this.loadTasks(),
        error: (error: unknown) => this.showActionError(error),
      });
  }

  onStatusChange(task: TaskResponse, status: TaskStatus): void {
    if (!this.canUpdateStatus(task) || task.status === status) {
      return;
    }

    const request: UpdateTaskStatusRequest = { status };
    this.actionLoading.set(task.id);
    this.tasksService
      .updateStatus(task.id, request)
      .pipe(finalize(() => this.actionLoading.set(null)))
      .subscribe({
        next: () => this.loadTasks(),
        error: (error: unknown) => this.showActionError(error),
      });
  }

  onPriorityChange(task: TaskResponse, priority: TaskPriority): void {
    if (!this.canManageTasks() || task.priority === priority) {
      return;
    }

    const request: UpdateTaskPriorityRequest = { priority };
    this.actionLoading.set(task.id);
    this.tasksService
      .updatePriority(task.id, request)
      .pipe(finalize(() => this.actionLoading.set(null)))
      .subscribe({
        next: () => this.loadTasks(),
        error: (error: unknown) => this.showActionError(error),
      });
  }

  canUpdateStatus(task: TaskResponse): boolean {
    if (this.canManageTasks()) {
      return true;
    }

    return (
      this.hasRole('TeamMember') && task.assignedToUserId === this.authService.currentUser()?.userId
    );
  }

  memberName(userId: string | null): string {
    if (!userId) {
      return 'Unassigned';
    }

    const member = this.members().find((projectMember) => projectMember.userId === userId);

    if (!member) {
      return 'Project member';
    }

    return `${member.firstName} ${member.lastName}`.trim() || member.email;
  }

  statusLabel(status: TaskStatus): string {
    return taskStatusLabel(status);
  }

  priorityLabel(priority: TaskPriority): string {
    return taskPriorityLabel(priority);
  }

  descriptionPreview(description: string | null): string {
    if (!description) {
      return 'No description';
    }

    return description.length > 96 ? `${description.slice(0, 93)}...` : description;
  }

  trackTask(_index: number, task: TaskResponse): string {
    return task.id;
  }

  private buildQuery(): TaskListQuery {
    const value = this.filtersForm.getRawValue();

    return {
      keyword: value.keyword.trim() || undefined,
      status: value.status ?? undefined,
      priority: value.priority ?? undefined,
      assignedUserId: value.assignedUserId || undefined,
      dueDateFrom: this.toApiDate(value.dueDateFrom),
      dueDateTo: this.toApiDate(value.dueDateTo),
      pageNumber: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      sortColumn: this.sortColumn(),
      sortDirection: this.sortDirection(),
    };
  }

  private toApiDate(value: Date | null): string | undefined {
    if (!value || Number.isNaN(value.getTime())) {
      return undefined;
    }

    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private runAction<T>(actionId: string, request: Observable<T>): Observable<T> {
    this.actionLoading.set(actionId);
    this.actionErrorMessage.set(null);

    return request.pipe(finalize(() => this.actionLoading.set(null)));
  }

  private showActionError(error: unknown): void {
    this.actionErrorMessage.set(this.apiErrorService.getMessage(error));
  }

  private hasRole(role: string): boolean {
    return this.authService
      .currentRoles()
      .some((currentRole) => currentRole.toLowerCase() === role.toLowerCase());
  }
}
