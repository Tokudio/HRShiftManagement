
import {
  Component,
  OnInit,
  inject,
  ChangeDetectorRef
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

import {
  UserManagementService,
  UserListItem
} from '../../../core/services/user-management.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.html',
  styleUrl: './users.scss',
})
export class Users implements OnInit {
  private userService = inject(UserManagementService);
  private cdr = inject(ChangeDetectorRef);
  private authService = inject(AuthService);
  private router = inject(Router);
  users: UserListItem[] = [];

  // Vai trò Admin đang chọn
  pendingRoles: Record<string, string> = {};

  // ID người đang đăng nhập
  currentUserId = '';

  isLoading = false;
  savingUserId: string | null = null;

  errorMessage = '';
  successMessage = '';

  readonly roles = [
    { label: 'Admin', value: 'Admin' },
    { label: 'Quản lý', value: 'Manager' },
    { label: 'Nhân viên', value: 'Employee' }
  ];

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadUsers();
  }
  logout(): void {
  this.authService.logout().subscribe({
    next: () => {
      console.log('Đăng xuất thành công');
      this.router.navigate(['/login']);
    },

    error: (error) => {
      console.error('Lỗi thu hồi Refresh Token:', error);

      // AuthService đã xóa session trong finalize()
      // kể cả khi API trả về lỗi.
      this.router.navigate(['/login']);
    }
  });
}

  loadCurrentUser(): void {
    const userJson = localStorage.getItem('user');

    if (userJson) {
      try {
        const user = JSON.parse(userJson);
        this.currentUserId = user.id;
      } catch {
        this.currentUserId = '';
      }
    }
  }

  loadUsers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.userService.getUsers().subscribe({
      next: (data) => {
        this.users = data;

        // Khởi tạo vai trò đang chọn bằng vai trò hiện tại
        this.pendingRoles = {};

        for (const user of data) {
          this.pendingRoles[user.id] = user.role;
        }

        this.isLoading = false;
        this.cdr.markForCheck();
      },

      error: (error) => {
        console.error('Lỗi lấy danh sách:', error);

        this.isLoading = false;

        this.errorMessage =
          error.error?.message ||
          'Không thể tải danh sách người dùng.';

        this.cdr.markForCheck();
      }
    });
  }
  

  saveUserRole(user: UserListItem): void {
    const newRole = this.pendingRoles[user.id];

    if (!newRole || newRole === user.role) {
      return;
    }

    if (user.id === this.currentUserId) {
      this.errorMessage = 'Bạn không thể tự thay đổi vai trò của mình.';
      return;
    }

    this.savingUserId = user.id;
    this.errorMessage = '';
    this.successMessage = '';

    this.userService.updateUserRole(user.id, newRole).subscribe({
      next: (response) => {
        this.savingUserId = null;
        this.successMessage = response.message ||
          'Cập nhật vai trò thành công.';

        this.loadUsers();
        this.cdr.markForCheck();
      },

      error: (error) => {
        this.savingUserId = null;

        this.errorMessage =
          error.error?.message ||
          'Cập nhật vai trò thất bại.';

        this.cdr.markForCheck();
      }
    });
    
  }
  
}
