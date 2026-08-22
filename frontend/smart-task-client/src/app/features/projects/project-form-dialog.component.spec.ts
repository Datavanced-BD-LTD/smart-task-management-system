import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { AuthService } from '../../core/services/auth.service';
import { ProjectResponse } from '../../core/models/project.model';
import { ProjectFormDialogComponent, ProjectFormDialogData } from './project-form-dialog.component';

describe('ProjectFormDialogComponent', () => {
  function createFixture(data: ProjectFormDialogData): {
    fixture: ComponentFixture<ProjectFormDialogComponent>;
    component: ProjectFormDialogComponent;
    close: ReturnType<typeof vi.fn>;
  } {
    const close = vi.fn();

    TestBed.configureTestingModule({
      imports: [ProjectFormDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close } },
        {
          provide: AuthService,
          useValue: {
            currentRoles: vi.fn(() => ['Admin']),
            currentUser: vi.fn(() => null),
          },
        },
      ],
    });

    const fixture = TestBed.createComponent(ProjectFormDialogComponent);
    fixture.detectChanges();

    return { fixture, component: fixture.componentInstance, close };
  }

  it('validates the required project name', () => {
    const { fixture, component, close } = createFixture({});

    component.submit();

    expect(component.form.invalid).toBe(true);
    expect(component.getError('name')).toBe('Project name is required.');
    expect(close).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).not.toContain('11111111-1111-4111-8111-111111111111');
  });

  it('loads existing project data for update', () => {
    const project = createProject();
    const { component } = createFixture({ project });

    expect(component.form.controls.name.value).toBe(project.name);
    expect(component.form.controls.description.value).toBe(project.description);
    expect(component.form.controls.projectManagerId.value).toBe(project.projectManagerId);
    expect(component.projectManagerDisplayName()).toBe('Unknown user');
  });
});

function createProject(): ProjectResponse {
  return {
    projectId: 'project-1',
    name: 'Alpha project',
    description: 'Description',
    projectManagerId: '11111111-1111-4111-8111-111111111111',
    createdByUserId: 'admin-1',
    createdAtUtc: '2026-08-22T00:00:00Z',
    updatedAtUtc: '2026-08-22T00:00:00Z',
  };
}
