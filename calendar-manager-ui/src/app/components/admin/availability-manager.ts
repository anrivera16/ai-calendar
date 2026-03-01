import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AdminApiService } from '../../services/admin-api.service';
import { AvailabilityRule, CreateWeeklyAvailability, CreateDateOverride, CreateBreak } from '../../models/admin.models';

@Component({
  selector: 'app-availability-manager',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './availability-manager.html',
  styleUrl: './availability-manager.scss',
})
export class AvailabilityManagerComponent implements OnInit {
  rules: AvailabilityRule[] = [];
  loading: boolean = true;
  error: string = '';
  success: string = '';

  // Weekly schedule form
  weeklySchedule: { [key: number]: { enabled: boolean; startTime: string; endTime: string } } = {};

  // Override form
  showOverrideForm: boolean = false;
  overrideDate: string = '';
  overrideStartTime: string = '09:00';
  overrideEndTime: string = '17:00';
  overrideIsAvailable: boolean = true;

  // Break form
  showBreakForm: boolean = false;
  breakDate: string = '';
  breakStartTime: string = '12:00';
  breakEndTime: string = '13:00';

  // Deleting rule
  deletingRuleId: string | null = null;

  days = [
    { value: 0, name: 'Sunday' },
    { value: 1, name: 'Monday' },
    { value: 2, name: 'Tuesday' },
    { value: 3, name: 'Wednesday' },
    { value: 4, name: 'Thursday' },
    { value: 5, name: 'Friday' },
    { value: 6, name: 'Saturday' },
  ];

  timeOptions: string[] = [];

  constructor(private adminApi: AdminApiService) {
    this.generateTimeOptions();
    this.initializeWeeklySchedule();
  }

  ngOnInit() {
    this.loadAvailability();
  }

  private generateTimeOptions() {
    for (let hour = 0; hour < 24; hour++) {
      for (let min = 0; min < 60; min += 30) {
        const time = `${hour.toString().padStart(2, '0')}:${min.toString().padStart(2, '0')}`;
        this.timeOptions.push(time);
      }
    }
  }

  private initializeWeeklySchedule() {
    for (let i = 0; i < 7; i++) {
      this.weeklySchedule[i] = {
        enabled: false,
        startTime: '09:00',
        endTime: '17:00',
      };
    }
  }

  loadAvailability() {
    this.loading = true;
    this.adminApi.getAvailability().subscribe({
      next: (rules) => {
        this.rules = rules;
        this.syncWeeklyScheduleFromRules();
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message;
        this.loading = false;
      },
    });
  }

  private syncWeeklyScheduleFromRules() {
    // Reset
    this.initializeWeeklySchedule();

    // Apply weekly rules
    const weeklyRules = this.rules.filter(r => r.ruleType === 'Weekly');
    for (const rule of weeklyRules) {
      if (rule.dayOfWeek !== undefined && rule.startTime && rule.endTime) {
        this.weeklySchedule[rule.dayOfWeek] = {
          enabled: rule.isAvailable,
          startTime: this.extractTime(rule.startTime),
          endTime: this.extractTime(rule.endTime),
        };
      }
    }
  }

  private extractTime(timeStr: string): string {
    // Handle "09:00:00" format
    return timeStr.substring(0, 5);
  }

  saveWeeklySchedule(dayOfWeek: number) {
    const schedule = this.weeklySchedule[dayOfWeek];
    const dto: CreateWeeklyAvailability = {
      dayOfWeek,
      startTime: schedule.startTime + ':00',
      endTime: schedule.endTime + ':00',
      isAvailable: schedule.enabled,
    };

    this.adminApi.createWeeklyAvailability(dto).subscribe({
      next: () => {
        this.showSuccess('Schedule updated successfully');
        this.loadAvailability();
      },
      error: (err) => {
        this.error = err.message;
      },
    });
  }

  toggleOverrideForm() {
    this.showOverrideForm = !this.showOverrideForm;
    this.showBreakForm = false;
    // Set default date to today
    if (!this.overrideDate) {
      this.overrideDate = new Date().toISOString().split('T')[0];
    }
  }

  toggleBreakForm() {
    this.showBreakForm = !this.showBreakForm;
    this.showOverrideForm = false;
    // Set default date to today
    if (!this.breakDate) {
      this.breakDate = new Date().toISOString().split('T')[0];
    }
  }

  saveOverride() {
    const dto: CreateDateOverride = {
      specificDate: this.overrideDate,
      startTime: this.overrideIsAvailable ? this.overrideStartTime + ':00' : undefined,
      endTime: this.overrideIsAvailable ? this.overrideEndTime + ':00' : undefined,
      isAvailable: this.overrideIsAvailable,
    };

    this.adminApi.createDateOverride(dto).subscribe({
      next: () => {
        this.showSuccess('Date override saved');
        this.showOverrideForm = false;
        this.loadAvailability();
      },
      error: (err) => {
        this.error = err.message;
      },
    });
  }

  saveBreak() {
    const dto: CreateBreak = {
      specificDate: this.breakDate,
      startTime: this.breakStartTime + ':00',
      endTime: this.breakEndTime + ':00',
    };

    this.adminApi.createBreak(dto).subscribe({
      next: () => {
        this.showSuccess('Break time saved');
        this.showBreakForm = false;
        this.loadAvailability();
      },
      error: (err) => {
        this.error = err.message;
      },
    });
  }

  deleteRule(ruleId: string) {
    if (!confirm('Are you sure you want to delete this availability rule?')) {
      return;
    }

    this.deletingRuleId = ruleId;
    this.adminApi.deleteAvailabilityRule(ruleId).subscribe({
      next: () => {
        this.deletingRuleId = null;
        this.loadAvailability();
      },
      error: (err) => {
        this.error = err.message;
        this.deletingRuleId = null;
      },
    });
  }

  getWeeklyRules(): AvailabilityRule[] {
    return this.rules.filter(r => r.ruleType === 'Weekly');
  }

  getOverrideRules(): AvailabilityRule[] {
    return this.rules.filter(r => r.ruleType === 'DateOverride');
  }

  getBreakRules(): AvailabilityRule[] {
    return this.rules.filter(r => r.ruleType === 'Break');
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
    });
  }

  private showSuccess(message: string) {
    this.success = message;
    setTimeout(() => {
      this.success = '';
    }, 3000);
  }
}
