import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';

import {
  ActivatedRoute,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';

// Cau truc mot muc trong Sidebar
interface WorkmateLink {
  label: string;
  icon: string;
  path?: string;
}

@Component({
  selector: 'app-workspace-layout',
  standalone: true,

  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],

  templateUrl: './workspace-layout.html',
  styleUrl: './workspace-layout.scss'
})
export class WorkspaceLayout {

  private activatedRoute = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);

  // Xac dinh giao dien Manager hay Employee
  role: 'manager' | 'employee' =
    this.activatedRoute.snapshot.data['workspace'] === 'employee'
      ? 'employee'
      : 'manager';

  name = 'Người dùng';

  // ============================================
  // MENU MANAGER
  // ============================================

  managerLinks: WorkmateLink[] = [
    {
      label: 'Tổng quan',
      icon: '▦',
      path: '/manager/dashboard'
    },
    {
      label: 'Nhân viên',
      icon: '♙',
      path: '/manager/employees'
    },
    {
      label: 'Ca & lịch làm việc',
      icon: '▤',
      path: '/manager/shifts'
    },
    {
      label: 'Phân nhân viên vào ca',
      icon: '▦',
      path: '/manager/schedule'
    },
    {
      label: 'Chấm công',
      icon: '◷'
    },
    {
      label: 'Nghỉ phép & đổi ca',
      icon: '▣'
    },
    {
      label: 'Lương & hiệu suất',
      icon: '▥'
    },
    {
      label: 'Báo cáo',
      icon: '▤'
    }
  ];

  // ============================================
  // MENU EMPLOYEE
  // ============================================

  employeeLinks: WorkmateLink[] = [
    {
      label: 'Tổng quan',
      icon: '▦',
      path: '/employee/dashboard'
    },
    {
      label: 'Lịch làm việc',
      icon: '▤',
      path: '/employee/schedule'
    },
    {
      label: 'Chấm công',
      icon: '◷'
    },
    {
      label: 'Nghỉ phép & đổi ca',
      icon: '▣'
    },
    {
      label: 'Thông báo',
      icon: '◉'
    },
    {
      label: 'Đồng bộ lịch cá nhân',
      icon: '▥'
    }
  ];

  // ============================================
  // LAY THONG TIN NGUOI DUNG
  // ============================================

  constructor() {
    try {
      const userJson = localStorage.getItem('user');

      if (userJson) {
        const user = JSON.parse(userJson);
        this.name = user.fullName || 'Người dùng';
      }
    } catch {
      this.name = 'Người dùng';
    }
  }

  // ============================================
  // LAY MENU THEO VAI TRO
  // ============================================

  get links(): WorkmateLink[] {
    return this.role === 'manager'
      ? this.managerLinks
      : this.employeeLinks;
  }

  // ============================================
  // TIEU DE TRANG
  // ============================================

  get title(): string {
    const path = this.router.url;

    if (path.includes('/employees/new')) {
      return 'Tạo hồ sơ nhân viên';
    }

    if (
      path.includes('/employees/') &&
      path.includes('/edit')
    ) {
      return 'Chỉnh sửa hồ sơ';
    }

    if (path.includes('/employees')) {
      return 'Nhân viên';
    }

    if (path.includes('/schedule')) {
      return this.role === 'manager'
        ? 'Phân nhân viên vào ca'
        : 'Lịch làm việc của tôi';
    }

    if (path.includes('/shifts')) {
      return 'Quản lý ca làm việc';
    }

    return this.role === 'manager'
      ? 'Tổng quan Manager'
      : 'Tổng quan Nhân viên';
  }

  // ============================================
  // CHU CAI DAI DIEN CHO AVATAR
  // ============================================

  get initials(): string {
    return this.name.trim().charAt(0).toUpperCase() || 'U';
  }

  // ============================================
  // DANG XUAT
  // ============================================

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/login']);
      },
      error: () => {
        this.router.navigate(['/login']);
      }
    });
  }
}
