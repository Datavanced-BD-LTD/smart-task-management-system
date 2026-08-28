import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import { AdminUsersService } from '../../core/services/admin-users.service';
import { AdminUsersPageComponent } from './admin-users-page.component';

describe('AdminUsersPageComponent', () => {
  let fixture: ComponentFixture<AdminUsersPageComponent>;
  let component: AdminUsersPageComponent;
  let service: {
    list: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    updateRole: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };
  let dialog: { open: ReturnType<typeof vi.fn> };

  const user = {
    userId: 'user-1',
    email: 'maria@example.com',
    firstName: 'Maria',
    lastName: 'Manager',
    displayName: 'Maria Manager',
    roles: ['ProjectManager'],
    isActive: true,
    createdAtUtc: '2026-08-28T00:00:00Z',
  };

  beforeEach(async () => {
    service = {
      list: vi.fn(() => of({ success: true, message: 'Success', data: {
        items: [user], pageNumber: 1, pageSize: 20, totalCount: 1, totalPages: 1,
      }, errors: null, traceId: 'test' })),
      create: vi.fn(() => of({ success: true, data: user })),
      update: vi.fn(() => of({ success: true, data: user })),
      updateRole: vi.fn(() => of({ success: true, data: user })),
      delete: vi.fn(() => of({ success: true, data: null })),
    };
    dialog = { open: vi.fn(() => ({ afterClosed: () => of(null) })) };

    await TestBed.configureTestingModule({
      imports: [AdminUsersPageComponent],
      providers: [
        { provide: AdminUsersService, useValue: service },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: { currentUser: vi.fn(() => ({ userId: 'admin-1' })) } },
        { provide: ApiErrorService, useValue: { getMessage: vi.fn(() => 'Unable to load users.'), getErrors: vi.fn(() => []) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminUsersPageComponent);
    component = fixture.componentInstance;
  });

  it('loads and renders friendly user identity and role', () => {
    fixture.detectChanges();

    expect(service.list).toHaveBeenCalledWith({ keyword: undefined, pageNumber: 1, pageSize: 20 });
    expect(fixture.nativeElement.textContent).toContain('Maria Manager');
    expect(fixture.nativeElement.textContent).toContain('Project Manager');
    expect(fixture.nativeElement.textContent).not.toContain('user-1');
  });

  it('opens the create-user dialog', () => {
    fixture.detectChanges();
    component.openCreateDialog();
    expect(dialog.open).toHaveBeenCalled();
  });

  it('displays safe API errors', () => {
    service.list.mockReturnValue(throwError(() => new Error('internal database details')));
    component.loadUsers();
    fixture.detectChanges();

    expect(component.errorMessage()).toBe('Unable to load users.');
    expect(fixture.nativeElement.textContent).not.toContain('internal database details');
  });

  it('edits a user profile through the admin API', () => {
    fixture.detectChanges();
    dialog.open.mockReturnValue({
      afterClosed: () => of({
        firstName: 'Maria',
        lastName: 'Updated',
        email: 'maria.updated@example.com',
      }),
    });

    component.openEditDialog(user);

    expect(service.update).toHaveBeenCalledWith('user-1', {
      firstName: 'Maria',
      lastName: 'Updated',
      email: 'maria.updated@example.com',
    });
  });

  it('deactivates a user only after confirmation', () => {
    fixture.detectChanges();
    dialog.open.mockReturnValue({ afterClosed: () => of(true) });

    component.confirmDeactivate(user);

    expect(service.delete).toHaveBeenCalledWith('user-1');
  });

  it('protects the signed-in administrator from role changes and deactivation', () => {
    fixture.detectChanges();
    const admin = { ...user, userId: 'admin-1', roles: ['Admin'] };

    expect(component.canChangeRole(admin)).toBe(false);
    expect(component.canDeactivate(admin)).toBe(false);
  });
});
