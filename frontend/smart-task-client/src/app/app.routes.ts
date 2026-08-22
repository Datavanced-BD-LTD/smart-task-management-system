import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
  {
    path: 'auth/login',
    loadComponent: () =>
      import('./features/auth/login-page.component').then(
        ({ LoginPageComponent }) => LoginPageComponent,
      ),
  },
  {
    path: '',
    component: AppShellComponent,
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
