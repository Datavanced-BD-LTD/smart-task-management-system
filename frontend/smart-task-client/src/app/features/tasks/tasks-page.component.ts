import { Component } from '@angular/core';
import { PlaceholderPageComponent } from '../../shared/components/placeholder-page/placeholder-page.component';

@Component({
  imports: [PlaceholderPageComponent],
  selector: 'app-tasks-page',
  template: `
    <app-placeholder-page
      title="Tasks"
      description="The tasks route is ready for task management screens and filters."
    />
  `,
})
export class TasksPageComponent {}
