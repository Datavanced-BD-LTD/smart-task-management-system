import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { AuthService } from '../core/services/auth.service';
import { NavigationItem } from '../shared/models/navigation-item.model';

@Component({
  imports: [
    MatButtonModule,
    MatSidenavModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  selector: 'app-shell',
  styleUrl: './app-shell.component.scss',
  templateUrl: './app-shell.component.html',
})
export class AppShellComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.authService.currentUser;
  readonly navigationItems: readonly NavigationItem[] = [
    { label: 'Dashboard', route: '/dashboard' },
    { label: 'Projects', route: '/projects' },
    { label: 'Tasks', route: '/tasks' },
  ];

  logout(): void {
    this.authService.logout().subscribe({
      complete: () => void this.router.navigate(['/auth/login']),
      error: () => void this.router.navigate(['/auth/login']),
    });
  }
}
