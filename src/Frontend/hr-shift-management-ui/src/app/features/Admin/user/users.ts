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
  UserListItem,
  CreateUserRequest,
  UpdateUserRequest
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

  // Danh sách tài khoản
  users: UserListItem[] = [];

  // Vai trò Admin đang chọn
  pendingRoles: Record<string, string> = {};

  // ID người đang đăng nhập
  currentUserId = '';

  // Trạng thái tải dữ liệu
  isLoading = false;

  // ID tài khoản đang được xử lý
  savingUserId: string | null = null;
  statusSavingUserId: string | null = null;

  // Trạng thái form tạo / chỉnh sửa
  showForm = false;
  isEditMode = false;
  editingUser: UserListItem | null = null;
  isSubmitting = false;

  // Dữ liệu form
  formData = {
    fullName: '',
    email: '',
    phoneNumber: '',
    password: '',
    role: 'Employee'
  };

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

  // Đăng xuất
  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/login']);
      },
      error: (error) => {
        console.error('Lỗi thu hồi Refresh Token:', error);
        this.router.navigate(['/login']);
      }
    });
  }

  // Lấy ID người đang đăng nhập
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

  // Tải danh sách tài khoản
  loadUsers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.userService.getUsers().subscribe({
      next: (data) => {
        this.users = data;

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

  // Mở form tạo tài khoản
  openCreateForm(): void {
    this.resetForm();

    this.isEditMode = false;
    this.editingUser = null;
    this.showForm = true;

    this.errorMessage = '';
    this.successMessage = '';
  }

  // Mở form chỉnh sửa tài khoản
  openEditForm(user: UserListItem): void {
    this.resetForm();

    this.isEditMode = true;
    this.editingUser = user;

    this.formData = {
      fullName: user.fullName,
      email: user.email,
      phoneNumber: user.phoneNumber || '',
      password: '',
      role: user.role
    };

    this.showForm = true;

    this.errorMessage = '';
    this.successMessage = '';
  }

  // Đóng form và xóa dữ liệu
  cancelForm(): void {
    this.showForm = false;
    this.editingUser = null;
    this.resetForm();
  }

  // Đưa form về trạng thái ban đầu
  resetForm(): void {
    this.formData = {
      fullName: '',
      email: '',
      phoneNumber: '',
      password: '',
      role: 'Employee'
    };
  }

  // Tạo tài khoản mới
  createUser(): void {
    if (
      !this.formData.fullName.trim() ||
      !this.formData.email.trim() ||
      !this.formData.password
    ) {
      this.errorMessage = 'Vui lòng nhập đầy đủ thông tin bắt buộc.';
      return;
    }

    const request: CreateUserRequest = {
      fullName: this.formData.fullName.trim(),
      email: this.formData.email.trim(),
      phoneNumber: this.formData.phoneNumber.trim() || null,
      password: this.formData.password,
      role: this.formData.role
    };

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.userService.createUser(request).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showForm = false;
        this.resetForm();

        this.successMessage = 'Tạo tài khoản thành công.';
        this.loadUsers();

        this.cdr.markForCheck();
      },

      error: (error) => {
        this.isSubmitting = false;

        this.errorMessage =
          error.error?.message ||
          'Tạo tài khoản thất bại. Vui lòng kiểm tra lại thông tin.';

        this.cdr.markForCheck();
      }
    });
  }

  // Cập nhật thông tin tài khoản
  updateUser(): void {
    if (!this.editingUser) {
      return;
    }

    if (
      !this.formData.fullName.trim() ||
      !this.formData.email.trim()
    ) {
      this.errorMessage = 'Họ tên và email không được để trống.';
      return;
    }

    const request: UpdateUserRequest = {
      fullName: this.formData.fullName.trim(),
      email: this.formData.email.trim(),
      phoneNumber: this.formData.phoneNumber.trim() || null
    };

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.userService.updateUser(
      this.editingUser.id,
      request
    ).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showForm = false;
        this.editingUser = null;
        this.resetForm();

        this.successMessage = 'Cập nhật thông tin thành công.';
        this.loadUsers();

        this.cdr.markForCheck();
      },

      error: (error) => {
        this.isSubmitting = false;

        this.errorMessage =
          error.error?.message ||
          'Cập nhật thông tin thất bại.';

        this.cdr.markForCheck();
      }
    });
  }

  // Lưu thay đổi vai trò
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

        this.successMessage =
          response.message || 'Cập nhật vai trò thành công.';

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

  // Khóa hoặc kích hoạt tài khoản
  toggleUserStatus(user: UserListItem): void {
    if (user.id === this.currentUserId && user.status === 'Active') {
      this.errorMessage = 'Bạn không thể tự khóa tài khoản của mình.';
      return;
    }

    const newStatus =
      user.status === 'Active' ? 'Locked' : 'Active';

    const actionText =
      newStatus === 'Locked' ? 'khóa' : 'kích hoạt';

    const confirmed = window.confirm(
      `Cậu có chắc chắn muốn ${actionText} tài khoản "${user.fullName}" không?`
    );

    if (!confirmed) {
      return;
    }

    this.statusSavingUserId = user.id;
    this.errorMessage = '';
    this.successMessage = '';

    this.userService.updateUserStatus(
      user.id,
      newStatus
    ).subscribe({
      next: (response) => {
        this.statusSavingUserId = null;

        this.successMessage =
          response.message ||
          `${newStatus === 'Locked' ? 'Khóa' : 'Kích hoạt'} tài khoản thành công.`;

        this.loadUsers();
        this.cdr.markForCheck();
      },

      error: (error) => {
        this.statusSavingUserId = null;

        this.errorMessage =
          error.error?.message ||
          `Không thể ${actionText} tài khoản.`;

        this.cdr.markForCheck();
      }
    });
  }

  // Hiển thị tên trạng thái
  getStatusLabel(status: string): string {
    switch (status) {
      case 'Active':
        return 'Đang hoạt động';

      case 'Locked':
        return 'Đã khóa';

      case 'Inactive':
        return 'Không hoạt động';

      default:
        return status;
    }
  }

  // Kiểm tra tài khoản có đang hoạt động không
  isUserActive(user: UserListItem): boolean {
    return user.status === 'Active';
  }
}