import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AdminApiService } from '../../services/admin-api.service';
import { Service, CreateService, UpdateService } from '../../models/admin.models';

@Component({
  selector: 'app-service-manager',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './service-manager.html',
  styleUrl: './service-manager.scss',
})
export class ServiceManagerComponent implements OnInit {
  services: Service[] = [];
  loading: boolean = true;
  error: string = '';

  // Modal state
  showModal: boolean = false;
  modalMode: 'create' | 'edit' = 'create';
  editingService: Service | null = null;

  // Form data
  formName: string = '';
  formDescription: string = '';
  formDurationMinutes: number = 30;
  formPrice: number = 0;
  formColor: string = '#3B82F6';
  formIsActive: boolean = true;
  formSortOrder: number = 0;

  // Form state
  saving: boolean = false;
  formError: string = '';

  // Delete confirmation
  showDeleteConfirm: boolean = false;
  deletingServiceId: string | null = null;

  // Color options
  colorOptions = [
    { value: '#3B82F6', name: 'Blue' },
    { value: '#10B981', name: 'Green' },
    { value: '#F59E0B', name: 'Amber' },
    { value: '#EF4444', name: 'Red' },
    { value: '#8B5CF6', name: 'Purple' },
    { value: '#EC4899', name: 'Pink' },
    { value: '#06B6D4', name: 'Cyan' },
    { value: '#6366F1', name: 'Indigo' },
  ];

  constructor(private adminApi: AdminApiService) {}

  ngOnInit() {
    this.loadServices();
  }

  loadServices() {
    this.loading = true;
    this.adminApi.getServices().subscribe({
      next: (services) => {
        this.services = services;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message;
        this.loading = false;
      },
    });
  }

  openCreateModal() {
    this.modalMode = 'create';
    this.editingService = null;
    this.resetForm();
    this.showModal = true;
  }

  openEditModal(service: Service) {
    this.modalMode = 'edit';
    this.editingService = service;
    this.formName = service.name;
    this.formDescription = service.description || '';
    this.formDurationMinutes = service.durationMinutes;
    this.formPrice = service.price;
    this.formColor = service.color;
    this.formIsActive = service.isActive;
    this.formSortOrder = service.sortOrder;
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
    this.editingService = null;
    this.resetForm();
  }

  resetForm() {
    this.formName = '';
    this.formDescription = '';
    this.formDurationMinutes = 30;
    this.formPrice = 0;
    this.formColor = '#3B82F6';
    this.formIsActive = true;
    this.formSortOrder = 0;
    this.formError = '';
  }

  onSubmit() {
    if (!this.formName.trim()) {
      this.formError = 'Service name is required';
      return;
    }

    this.formError = '';
    this.saving = true;

    const serviceData: CreateService | UpdateService = {
      name: this.formName,
      description: this.formDescription || undefined,
      durationMinutes: this.formDurationMinutes,
      price: this.formPrice,
      color: this.formColor,
      isActive: this.formIsActive,
      sortOrder: this.formSortOrder,
    };

    if (this.modalMode === 'edit' && this.editingService) {
      this.adminApi.updateService(this.editingService.id, serviceData).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadServices();
        },
        error: (err) => {
          this.formError = err.message;
          this.saving = false;
        },
      });
    } else {
      this.adminApi.createService(serviceData as CreateService).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadServices();
        },
        error: (err) => {
          this.formError = err.message;
          this.saving = false;
        },
      });
    }
  }

  confirmDelete(serviceId: string) {
    this.deletingServiceId = serviceId;
    this.showDeleteConfirm = true;
  }

  cancelDelete() {
    this.deletingServiceId = null;
    this.showDeleteConfirm = false;
  }

  deleteService() {
    if (!this.deletingServiceId) return;

    this.adminApi.deleteService(this.deletingServiceId).subscribe({
      next: () => {
        this.deletingServiceId = null;
        this.showDeleteConfirm = false;
        this.loadServices();
      },
      error: (err) => {
        this.error = err.message;
        this.deletingServiceId = null;
        this.showDeleteConfirm = false;
      },
    });
  }

  formatDuration(minutes: number): string {
    if (minutes < 60) return `${minutes} min`;
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(price);
  }
}
