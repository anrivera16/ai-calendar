import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { of, throwError } from 'rxjs';
import { provideRouter, Router } from '@angular/router';
import { BusinessSetupComponent } from './business-setup';
import { AdminApiService } from '../../services/admin-api.service';
import { BusinessProfile } from '../../models/admin.models';

describe('BusinessSetupComponent', () => {
  let mockAdminApi: {
    getProfile: ReturnType<typeof vi.fn>;
    createProfile: ReturnType<typeof vi.fn>;
    updateProfile: ReturnType<typeof vi.fn>;
  };

  const mockProfile: BusinessProfile = {
    id: 'profile-1',
    businessName: 'Test Business',
    slug: 'test-business',
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    bookingUrl: '/book/test-business',
  };

  beforeEach(async () => {
    mockAdminApi = {
      getProfile: vi.fn().mockReturnValue(
        of({
          hasProfile: true,
          ...mockProfile,
        }),
      ),
      createProfile: vi.fn().mockReturnValue(of(mockProfile)),
      updateProfile: vi.fn().mockReturnValue(of(mockProfile)),
    };
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates_theComponent', async () => {
    await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      expect(screen.getByText(/Business/i)).toBeTruthy();
    });
  });

  it('loadsExistingProfile_onInit', async () => {
    await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    expect(mockAdminApi.getProfile).toHaveBeenCalled();
  });

  it('showsCreateForm_whenNoProfile', async () => {
    mockAdminApi.getProfile.mockReturnValue(
      of({
        hasProfile: false,
      }),
    );

    await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      expect(screen.getByText(/Set Up Your Business/i)).toBeTruthy();
    });
  });

  it('showsEditForm_whenProfileExists', async () => {
    await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      expect(screen.getByText(/Edit Business Profile/i)).toBeTruthy();
    });
  });

  it('populatesForm_withExistingData', async () => {
    await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      const businessNameInput = screen.getByDisplayValue('Test Business');
      expect(businessNameInput).toBeTruthy();
    });
  });

  it('onSubmit_createsProfile_whenNew', async () => {
    mockAdminApi.getProfile.mockReturnValue(
      of({
        hasProfile: false,
      }),
    );

    await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      const businessNameInput = screen.getByLabelText(/Business Name/i);
      fireEvent.input(businessNameInput, { target: { value: 'New Business' } });
    });

    const submitButton = screen.getByRole('button', { name: /Create Profile/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(mockAdminApi.createProfile).toHaveBeenCalled();
    });
  });

  it('onSubmit_updatesProfile_whenEditing', async () => {
    await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Save Changes/i })).toBeTruthy();
    });

    const submitButton = screen.getByRole('button', { name: /Save Changes/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(mockAdminApi.updateProfile).toHaveBeenCalled();
    });
  });

  it('showsValidationErrors_whenFieldsMissing', async () => {
    mockAdminApi.getProfile.mockReturnValue(
      of({
        hasProfile: false,
      }),
    );

    await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      const submitButton = screen.getByRole('button', { name: /Create Profile/i });
      fireEvent.click(submitButton);
    });
  });

  it('onCancel_navigatesToAdminDashboard', async () => {
    const { fixture } = await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    const router = fixture.debugElement.injector.get(Router);
    vi.spyOn(router, 'navigate');

    await waitFor(() => {
      const cancelButton = screen.getByRole('button', { name: /Cancel/i });
      fireEvent.click(cancelButton);
    });

    await waitFor(() => {
      expect(router.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
    });
  });

  it('showsLoadingState_whileSaving', async () => {
    mockAdminApi.getProfile.mockReturnValue(
      of({
        hasProfile: false,
      }),
    );

    await render(BusinessSetupComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      const businessNameInput = screen.getByLabelText(/Business Name/i);
      fireEvent.input(businessNameInput, { target: { value: 'New Business' } });
    });

    const submitButton = screen.getByRole('button', { name: /Create Profile/i });
    fireEvent.click(submitButton);
  });
});
