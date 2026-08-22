import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  imports: [MatCardModule],
  selector: 'app-login-page',
  template: `
    <main class="auth-page">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Sign in</mat-card-title>
          <mat-card-subtitle>Authentication UI placeholder</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>The login form will be implemented in the authentication phase.</p>
        </mat-card-content>
      </mat-card>
    </main>
  `,
  styles: `
    .auth-page {
      display: grid;
      min-height: 100dvh;
      padding: 1rem;
      place-items: center;
      background: #f5f7fb;
    }

    mat-card {
      width: min(100%, 28rem);
    }

    p {
      color: #5f6368;
      line-height: 1.6;
    }
  `,
})
export class LoginPageComponent {}
