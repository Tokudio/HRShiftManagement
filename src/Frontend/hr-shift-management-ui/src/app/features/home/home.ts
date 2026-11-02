
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  templateUrl: './home.html',
})
export class Home {
  private authService = inject(AuthService);
  private router = inject(Router);

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/login']);
      },
      error: (error) => {
        console.error('Không thể thu hồi Refresh Token:', error);

        // Dù API thất bại, người dùng vẫn được đăng xuất ở Frontend.
        this.router.navigate(['/login']);
      },
    });
  }
}
