import { Component } from '@angular/core';
import { PlaceholderPageComponent } from '../../shared/components/placeholder-page/placeholder-page.component';

@Component({
  imports: [PlaceholderPageComponent],
  selector: 'app-projects-page',
  template: `
    <app-placeholder-page
      title="Projects"
      description="The projects route is ready for project management screens."
    />
  `,
})
export class ProjectsPageComponent {}
