import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { of, throwError } from 'rxjs';
import { provideRouter } from '@angular/router';
import { ServiceManagerComponent } from './service-manager';
import { AdminApiService } from '../../services/admin-api.service';
import { Service } from '../../models/admin.models';

describe('ServiceManagerComponent', () => {
  let mockAdminApi: {
    getServices: ReturnType<typeof vi.fn>;
    createService: ReturnType<typeof vi.fn>;
    updateService: ReturnType<typeof vi.fn>;
    deleteService: ReturnType<typeof vi.fn>;
  };

  const mockServices: Service[] = [
    {
      id: 'service-1',
      businessProfileId: 'profile-1',
      name: 'Haircut',
      description: 'Basic haircut service',
      durationMinutes: 30,
      price: 25,
      color: '#3B82F6',
      isActive: true,
      sortOrder: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 'service-2',
      businessProfileId: 'profile-1',
      name: 'Beard Trim',
      description: 'Beard trimming',
      durationMinutes: 15,
      price: 15,
      color: '#10B981',
      isActive: true,
      sortOrder: 1,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
  ];

  beforeEach(async () => {
    mockAdminApi = {
      getServices: vi.fn().mockReturnValue(of(mockServices)),
      createService: vi.fn().mockReturnValue(of(mockServices[0])),
      updateService: vi.fn().mockReturnValue(of(mockServices[0])),
      deleteService: vi.fn().mockReturnValue(of({ message: 'Deleted' })),
    };
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates_theComponent', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/Services/i)).toBeTruthy();
    });
  });

  it('loadsServices_onInit', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    expect(mockAdminApi.getServices).toHaveBeenCalled();
  });

  it('rendersServiceList', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
      expect(screen.getByText('Beard Trim')).toBeTruthy();
    });
  });

  it('openCreateModal_showsEmptyForm', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('+ Add Service')).toBeTruthy();
    });

    const addButton = screen.getByText('+ Add Service');
    fireEvent.click(addButton);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Add Service/i })).toBeTruthy();
    });
  });

  it('openEditModal_populatesFormWithServiceData', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const editButtons = screen.getAllByTitle('Edit');
    fireEvent.click(editButtons[0]);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Haircut')).toBeTruthy();
    });
  });

  it('closeModal_hidesForm', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('+ Add Service')).toBeTruthy();
    });

    const addButton = screen.getByText('+ Add Service');
    fireEvent.click(addButton);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Add Service/i })).toBeTruthy();
    });

    const cancelButton = screen.getByRole('button', { name: /Cancel/i });
    fireEvent.click(cancelButton);

    await waitFor(() => {
      expect(screen.queryByRole('heading', { name: /Add Service/i })).toBeFalsy();
    });
  });

  it('onSubmit_createsNewService', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('+ Add Service')).toBeTruthy();
    });

    const addButton = screen.getByText('+ Add Service');
    fireEvent.click(addButton);

    await waitFor(() => {
      const nameInput = screen.getByLabelText(/Service Name/i);
      fireEvent.input(nameInput, { target: { value: 'New Service' } });
    });

    const submitButtons = screen.getAllByRole('button', { name: /Add Service/i });
    fireEvent.click(submitButtons[submitButtons.length - 1]);

    await waitFor(() => {
      expect(mockAdminApi.createService).toHaveBeenCalled();
    });
  });

  it('onSubmit_updatesExistingService', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const editButtons = screen.getAllByTitle('Edit');
    fireEvent.click(editButtons[0]);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Edit Service/i })).toBeTruthy();
    });

    const saveButton = screen.getByRole('button', { name: /Save Changes/i });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(mockAdminApi.updateService).toHaveBeenCalled();
    });
  });

  it('confirmDelete_showsConfirmation', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const deleteButtons = screen.getAllByTitle('Delete');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Delete Service/i })).toBeTruthy();
    });
  });

  it('deleteService_removesFromList', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const deleteButtons = screen.getAllByTitle('Delete');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Delete Service/i })).toBeTruthy();
    });

    const confirmButton = screen.getByRole('button', { name: /Delete/i });
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockAdminApi.deleteService).toHaveBeenCalled();
    });
  });

  it('cancelDelete_hidesConfirmation', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const deleteButtons = screen.getAllByTitle('Delete');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Delete Service/i })).toBeTruthy();
    });

    const cancelButton = screen.getByRole('button', { name: /Cancel/i });
    fireEvent.click(cancelButton);

    await waitFor(() => {
      expect(screen.queryByRole('heading', { name: /Delete Service/i })).toBeFalsy();
    });
  });

  it('formatsPrice_asCurrency', async () => {
    const { fixture } = await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    const component = fixture.componentInstance;
    const formatted = component.formatPrice(25);
    expect(formatted).toBe('$25.00');
  });

  it('formatsDuration_correctly', async () => {
    const { fixture } = await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    const component = fixture.componentInstance;

    expect(component.formatDuration(30)).toBe('30 min');
    expect(component.formatDuration(60)).toBe('1 hour');
    expect(component.formatDuration(90)).toBe('1.5 hours');
  });

  it('showsColorOptions', async () => {
    await render(ServiceManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('+ Add Service')).toBeTruthy();
    });

    const addButton = screen.getByText('+ Add Service');
    fireEvent.click(addButton);

    await waitFor(() => {
      const colorSection = screen.getByText(/Color/i);
      expect(colorSection).toBeTruthy();
    });
  });
});
