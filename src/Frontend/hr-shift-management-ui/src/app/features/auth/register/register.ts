import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, RegisterRequest } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private authService = inject(AuthService);
  private router = inject(Router);

  fullName = '';
  email = '';
  phoneNumber = '';
  password = '';
  confirmPassword = '';
  showPassword = false;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  // Chỉ cho phép nhập số điện thoại từ 0 đến 9
  // Chỉ cho phép nhập các chữ số từ 0 đến 9
  onPhoneNumberInput(event: Event): void {
    const input = event.target as HTMLInputElement;

    // Loại bỏ chữ cái và ký tự đặc biệt
    const numericValue = input.value.replace(/[^0-9]/g, '');

    // Cập nhật trực tiếp nội dung ô input
    input.value = numericValue;

    // Cập nhật dữ liệu trong Angular
    this.phoneNumber = numericValue;
  }
  onRegister(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (
      !this.fullName.trim() ||
      !this.email.trim() ||
      !this.password ||
      !this.confirmPassword
    ) {
      this.errorMessage = 'Vui lòng nhập đầy đủ các trường bắt buộc.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Mật khẩu xác nhận không khớp.';
      return;
    }

    const passwordPattern =
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$/;

    if (!passwordPattern.test(this.password)) {
      this.errorMessage =
        'Mật khẩu cần ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.';
      return;
    }

    this.isLoading = true;

    this.authService.register({
      fullName: this.fullName.trim(),
      email: this.email.trim(),
      password: this.password,
      confirmPassword: this.confirmPassword,
      phoneNumber: this.phoneNumber.trim() || null,
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Đăng ký thành công! Đang chuyển đến trang đăng nhập.';

        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1200);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage =
          error.error?.message ||
          'Đăng ký thất bại. Vui lòng kiểm tra lại thông tin.';
      },
    });
  }
}
