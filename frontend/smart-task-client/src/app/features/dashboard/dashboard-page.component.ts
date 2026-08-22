import { Component } from '@angular/core';
import { PlaceholderPageComponent } from '../../shared/components/placeholder-page/placeholder-page.component';

@Component({
  imports: [PlaceholderPageComponent],
  selector: 'app-dashboard-page',
  template: `
    <app-placeholder-page
      title="Dashboard"
      description="The dashboard foundation is ready for summary statistics and upcoming task widgets."
    />
  `,
})
export class DashboardPageComponent {}
