import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { AdminUserFormDialogComponent } from './admin-user-form-dialog.component';

describe('AdminUserFormDialogComponent', () => {
  function createFixture(data: object): {
    fixture: ComponentFixture<AdminUserFormDialogComponent>;
    component: AdminUserFormDialogComponent;
    close: ReturnType<typeof vi.fn>;
  } {
    const close = vi.fn();
    TestBed.configureTestingModule({
      imports: [AdminUserFormDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close } },
      ],
    });
    const fixture = TestBed.createComponent(AdminUserFormDialogComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, close };
  }

  it('validates required fields when creating a user', () => {
    const { component, close } = createFixture({});

    component.submit();

    expect(component.form.invalid).toBe(true);
    expect(close).not.toHaveBeenCalled();
  });

  it('allows changing an existing user role without a password', () => {
    const { component, close } = createFixture({
      user: {
        userId: 'user-1',
        email: 'maria@example.com',
        firstName: 'Maria',
        lastName: 'Manager',
        displayName: 'Maria Manager',
        roles: ['TeamMember'],
        isActive: true,
        createdAtUtc: '2026-08-28T00:00:00Z',
      },
    });

    component.submit();

    expect(close).toHaveBeenCalledWith({ role: 'TeamMember' });
  });
});
