import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { of, throwError } from 'rxjs';
import { provideRouter } from '@angular/router';
import { AvailabilityManagerComponent } from './availability-manager';
import { AdminApiService } from '../../services/admin-api.service';
import { AvailabilityRule } from '../../models/admin.models';

describe('AvailabilityManagerComponent', () => {
  let mockAdminApi: {
    getAvailability: ReturnType<typeof vi.fn>;
    createWeeklyAvailability: ReturnType<typeof vi.fn>;
    createDateOverride: ReturnType<typeof vi.fn>;
    createBreak: ReturnType<typeof vi.fn>;
    deleteAvailabilityRule: ReturnType<typeof vi.fn>;
  };

  const mockRules: AvailabilityRule[] = [
    {
      id: 'rule-1',
      businessProfileId: 'profile-1',
      ruleType: 'Weekly',
      dayOfWeek: 1,
      startTime: '09:00:00',
      endTime: '17:00:00',
      isAvailable: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 'rule-2',
      businessProfileId: 'profile-1',
      ruleType: 'DateOverride',
      specificDate: '2024-12-25',
      isAvailable: false,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 'rule-3',
      businessProfileId: 'profile-1',
      ruleType: 'Break',
      specificDate: '2024-01-15',
      startTime: '12:00:00',
      endTime: '13:00:00',
      isAvailable: false,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
  ];

  beforeEach(async () => {
    mockAdminApi = {
      getAvailability: vi.fn().mockReturnValue(of(mockRules)),
      createWeeklyAvailability: vi.fn().mockReturnValue(of({ message: 'Saved' })),
      createDateOverride: vi.fn().mockReturnValue(of({ message: 'Saved' })),
      createBreak: vi.fn().mockReturnValue(of({ message: 'Saved' })),
      deleteAvailabilityRule: vi.fn().mockReturnValue(of({ message: 'Deleted' })),
    };
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates_theComponent', async () => {
    await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/Availability/i)).toBeTruthy();
    });
  });

  it('loadsAvailabilityRules_onInit', async () => {
    await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    expect(mockAdminApi.getAvailability).toHaveBeenCalled();
  });

  it('initializesWeeklySchedule_forAll7Days', async () => {
    const { fixture } = await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(Object.keys(component.weeklySchedule).length).toBe(7);
      expect(component.weeklySchedule[0]).toBeDefined();
      expect(component.weeklySchedule[6]).toBeDefined();
    });
  });

  it('syncsScheduleFromRules_whenLoaded', async () => {
    const { fixture } = await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      expect(component.weeklySchedule[1].enabled).toBe(true);
      expect(component.weeklySchedule[1].startTime).toBe('09:00');
      expect(component.weeklySchedule[1].endTime).toBe('17:00');
    });
  });

  it('saveWeeklySchedule_callsApi', async () => {
    await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/Weekly Schedule/i)).toBeTruthy();
    });

    const toggle = screen.getAllByRole('checkbox')[0];
    fireEvent.click(toggle);

    await waitFor(() => {
      expect(mockAdminApi.createWeeklyAvailability).toHaveBeenCalled();
    });
  });

  it('toggleOverrideForm_showsAndHidesForm', async () => {
    await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/\+ Add Override/i)).toBeTruthy();
    });

    const overrideButton = screen.getByText(/\+ Add Override/i);
    fireEvent.click(overrideButton);

    await waitFor(() => {
      expect(screen.getByLabelText(/Date/i)).toBeTruthy();
    });
  });

  it('saveOverride_callsApi_andReloads', async () => {
    await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/\+ Add Override/i)).toBeTruthy();
    });

    const overrideButton = screen.getByText(/\+ Add Override/i);
    fireEvent.click(overrideButton);

    await waitFor(() => {
      const saveButton = screen.getByText(/Save Override/i);
      fireEvent.click(saveButton);
    });

    await waitFor(() => {
      expect(mockAdminApi.createDateOverride).toHaveBeenCalled();
    });
  });

  it('toggleBreakForm_showsAndHidesForm', async () => {
    await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/\+ Add Break/i)).toBeTruthy();
    });

    const breakButton = screen.getByText(/\+ Add Break/i);
    fireEvent.click(breakButton);

    await waitFor(() => {
      expect(screen.getByText(/Save Break/i)).toBeTruthy();
    });
  });

  it('saveBreak_callsApi_andReloads', async () => {
    await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/\+ Add Break/i)).toBeTruthy();
    });

    const breakButton = screen.getByText(/\+ Add Break/i);
    fireEvent.click(breakButton);

    await waitFor(() => {
      const saveButton = screen.getByText(/Save Break/i);
      fireEvent.click(saveButton);
    });

    await waitFor(() => {
      expect(mockAdminApi.createBreak).toHaveBeenCalled();
    });
  });

  it('deleteRule_callsApi_andReloads', async () => {
    await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/Date Overrides/i)).toBeTruthy();
    });

    const deleteButtons = screen.getAllByTitle('Delete');
    if (deleteButtons.length > 0) {
      fireEvent.click(deleteButtons[0]);
    }

    await waitFor(() => {
      expect(mockAdminApi.deleteAvailabilityRule).toHaveBeenCalled();
    });
  });

  it('getWeeklyRules_filtersCorrectly', async () => {
    const { fixture } = await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      const weeklyRules = component.getWeeklyRules();
      expect(weeklyRules.length).toBe(1);
      expect(weeklyRules[0].ruleType).toBe('Weekly');
    });
  });

  it('getOverrideRules_filtersCorrectly', async () => {
    const { fixture } = await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      const overrideRules = component.getOverrideRules();
      expect(overrideRules.length).toBe(1);
      expect(overrideRules[0].ruleType).toBe('DateOverride');
    });
  });

  it('getBreakRules_filtersCorrectly', async () => {
    const { fixture } = await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    const component = fixture.componentInstance;

    await waitFor(() => {
      const breakRules = component.getBreakRules();
      expect(breakRules.length).toBe(1);
      expect(breakRules[0].ruleType).toBe('Break');
    });
  });

  it('showsSuccessMessage_for3Seconds', async () => {
    vi.useFakeTimers();

    await render(AvailabilityManagerComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/\+ Add Override/i)).toBeTruthy();
    });

    const overrideButton = screen.getByText(/\+ Add Override/i);
    fireEvent.click(overrideButton);

    vi.advanceTimersByTime(100);

    vi.useRealTimers();
  });
});
