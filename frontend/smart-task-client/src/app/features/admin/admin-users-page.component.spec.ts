import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AdminUsersService } from '../../core/services/admin-users.service';
import { AdminUsersPageComponent } from './admin-users-page.component';

describe('AdminUsersPageComponent', () => {
  let fixture: ComponentFixture<AdminUsersPageComponent>;
  let component: AdminUsersPageComponent;
  let service: { list: ReturnType<typeof vi.fn>; create: ReturnType<typeof vi.fn>; updateRole: ReturnType<typeof vi.fn> };
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
      updateRole: vi.fn(() => of({ success: true, data: user })),
    };
    dialog = { open: vi.fn(() => ({ afterClosed: () => of(null) })) };

    await TestBed.configureTestingModule({
      imports: [AdminUsersPageComponent],
      providers: [
        { provide: AdminUsersService, useValue: service },
        { provide: MatDialog, useValue: dialog },
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
});
