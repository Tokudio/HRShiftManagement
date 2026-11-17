
import {
  Component,
  OnInit,
  inject,
  ChangeDetectorRef
} from '@angular/core';

import { CommonModule } from '@angular/common';

import {
  UserManagementService,
  UserListItem
} from '../../../core/services/user-management.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  private userService = inject(UserManagementService);
  private cdr = inject(ChangeDetectorRef);

  users: UserListItem[] = [];
  isLoading = true;
  errorMessage = '';

  get totalUsers(): number {
    return this.users.length;
  }

  get activeUsers(): number {
    return this.users.filter(u => u.status === 'Active').length;
  }

  get lockedUsers(): number {
    return this.users.filter(u => u.status === 'Locked').length;
  }

  get totalRoles(): number {
    return new Set(this.users.map(u => u.role)).size;
  }

  get adminCount(): number {
    return this.users.filter(u => u.role === 'Admin').length;
  }

  get managerCount(): number {
    return this.users.filter(u => u.role === 'Manager').length;
  }

  get employeeCount(): number {
    return this.users.filter(u => u.role === 'Employee').length;
  }

  get recentUsers(): UserListItem[] {
    return this.users.slice(0, 5);
  }

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.cdr.detectChanges();

    this.userService.getUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.isLoading = false;

        // Cập nhật giao diện ngay sau khi nhận dữ liệu
        this.cdr.detectChanges();
      },

      error: (error) => {
        console.error('Dashboard loading error:', error);

        this.errorMessage = 'Không thể tải dữ liệu tổng quan.';
        this.isLoading = false;

        // Cập nhật giao diện khi xảy ra lỗi
        this.cdr.detectChanges();
      }
    });
  }

  getPercentage(count: number): number {
    return this.totalUsers
      ? (count / this.totalUsers) * 100
      : 0;
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Active':
        return 'Hoạt động';

      case 'Locked':
        return 'Đã khóa';

      case 'Inactive':
        return 'Không hoạt động';

      default:
        return status;
    }
  }
}
