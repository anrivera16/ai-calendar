import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AdminApiService } from '../../services/admin-api.service';
import { BusinessProfile, CreateBusinessProfile, UpdateBusinessProfile } from '../../models/admin.models';

@Component({
  selector: 'app-business-setup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './business-setup.html',
  styleUrl: './business-setup.scss',
})
export class BusinessSetupComponent implements OnInit {
  // Form data
  businessName: string = '';
  description: string = '';
  phone: string = '';
  website: string = '';
  address: string = '';
  logoUrl: string = '';

  // State
  loading: boolean = true;
  saving: boolean = false;
  error: string = '';
  success: string = '';

  // Mode
  isEditMode: boolean = false;
  hasProfile: boolean = false;

  constructor(
    private adminApi: AdminApiService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadProfile();
  }

  private loadProfile() {
    this.adminApi.getProfile().subscribe({
      next: (response) => {
        this.hasProfile = response.hasProfile;
        this.isEditMode = response.hasProfile;

        if (response.hasProfile) {
          this.businessName = response.businessName || '';
          this.description = response.description || '';
          this.phone = response.phone || '';
          this.website = response.website || '';
          this.address = response.address || '';
          this.logoUrl = response.logoUrl || '';
        }

        this.loading = false;
      },
      error: (err) => {
        this.error = err.message;
        this.loading = false;
      },
    });
  }

  onSubmit() {
    if (!this.businessName.trim()) {
      this.error = 'Business name is required';
      return;
    }

    this.error = '';
    this.saving = true;

    if (this.isEditMode) {
      const updateData: UpdateBusinessProfile = {
        businessName: this.businessName,
        description: this.description || undefined,
        phone: this.phone || undefined,
        website: this.website || undefined,
        address: this.address || undefined,
        logoUrl: this.logoUrl || undefined,
      };

      this.adminApi.updateProfile(updateData).subscribe({
        next: (profile) => {
          this.success = 'Business profile updated successfully!';
          this.saving = false;
          setTimeout(() => {
            this.router.navigate(['/admin/dashboard']);
          }, 1500);
        },
        error: (err) => {
          this.error = err.message;
          this.saving = false;
        },
      });
    } else {
      const createData: CreateBusinessProfile = {
        businessName: this.businessName,
        description: this.description || undefined,
        phone: this.phone || undefined,
        website: this.website || undefined,
        address: this.address || undefined,
        logoUrl: this.logoUrl || undefined,
      };

      this.adminApi.createProfile(createData).subscribe({
        next: (profile) => {
          this.success = 'Business profile created successfully!';
          this.saving = false;
          setTimeout(() => {
            this.router.navigate(['/admin/dashboard']);
          }, 1500);
        },
        error: (err) => {
          this.error = err.message;
          this.saving = false;
        },
      });
    }
  }

  onCancel() {
    this.router.navigate(['/admin/dashboard']);
  }
}
