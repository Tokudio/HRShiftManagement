
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  showPassword = false;
  isLoading = false;
  errorMessage = '';

  onLogin(): void {
    this.errorMessage = '';

    if (!this.email.trim() || !this.password) {
      this.errorMessage = 'Vui lòng nhập email và mật khẩu.';
      return;
    }

    this.isLoading = true;

    this.authService.login({
      email: this.email.trim(),
      password: this.password,
    }).subscribe({
      next: (response) => {
        this.isLoading = false;

        console.log('LOGIN NEXT: API trả về thành công');
        console.log('User:', response.user);
        console.log('Role:', response.user.role);

        if (response.user.role?.toLowerCase() === 'admin') {
          this.router.navigate(['/admin/users']);
        } else {
          this.router.navigate(['/home']);
        }
      },
      error: (error) => {
        console.error('LOGIN ERROR:', error);

        this.isLoading = false;
        this.errorMessage =
          error.error?.message ||
          'Đăng nhập thất bại. Vui lòng kiểm tra email và mật khẩu.';
      },
      complete: () => {
        console.log('LOGIN COMPLETE');
      },
    });
  }
}
