import { Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';

@Component({
  imports: [MatButtonModule, MatCardModule, RouterLink],
  selector: 'app-placeholder-page',
  template: `
    <section class="placeholder-page">
      <mat-card>
        <mat-card-header>
          <mat-card-title>{{ title() }}</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>{{ description() }}</p>
        </mat-card-content>
        <mat-card-actions>
          <a mat-flat-button routerLink="/dashboard">Return to dashboard</a>
        </mat-card-actions>
      </mat-card>
    </section>
  `,
  styles: `
    .placeholder-page {
      display: grid;
      min-height: 18rem;
      place-items: center;
    }

    mat-card {
      width: min(100%, 38rem);
    }

    p {
      color: #5f6368;
      line-height: 1.6;
    }
  `,
})
export class PlaceholderPageComponent {
  readonly title = input.required<string>();
  readonly description = input(
    'This route is ready for the next implementation phase.',
  );
}
