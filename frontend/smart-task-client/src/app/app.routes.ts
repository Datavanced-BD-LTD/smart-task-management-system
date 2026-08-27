import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
  // Guest-only routes keep authenticated users out of login/register, while feature
  // components are lazy-loaded so their code is downloaded only when navigated to.
  {
    path: 'auth/login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login-page.component').then(
        ({ LoginPageComponent }) => LoginPageComponent,
      ),
  },
  {
    path: 'auth/register',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/register-page.component').then(
        ({ RegisterPageComponent }) => RegisterPageComponent,
      ),
  },
  {
    path: '',
    component: AppShellComponent,
    // The shell protects every child route. UI guards improve navigation only; the
    // backend remains the final authority for every role and resource operation.
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard-page.component').then(
            ({ DashboardPageComponent }) => DashboardPageComponent,
          ),
      },
      {
        path: 'projects',
        loadComponent: () =>
          import('./features/projects/projects-page.component').then(
            ({ ProjectsPageComponent }) => ProjectsPageComponent,
          ),
      },
      {
        path: 'projects/:projectId/tasks',
        loadComponent: () =>
          import('./features/tasks/tasks-page.component').then(
            ({ TasksPageComponent }) => TasksPageComponent,
          ),
      },
      {
        path: 'projects/:projectId',
        loadComponent: () =>
          import('./features/projects/project-details-page.component').then(
            ({ ProjectDetailsPageComponent }) => ProjectDetailsPageComponent,
          ),
      },
      {
        path: 'tasks',
        loadComponent: () =>
          import('./features/tasks/tasks-page.component').then(
            ({ TasksPageComponent }) => TasksPageComponent,
          ),
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
