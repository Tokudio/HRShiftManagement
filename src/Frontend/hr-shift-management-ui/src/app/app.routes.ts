import { inject } from '@angular/core';

import {
  CanActivateFn,
  Router,
  Routes
} from '@angular/router';

import {
  authGuard,
  adminGuard,
  employeeGuard
} from './core/guards/auth-guard';


// ============================================
// GUARD - CHI MANAGER DUOC TRUY CAP
// ============================================

const managerGuard: CanActivateFn = () => {

  const router = inject(Router);

  const token = localStorage.getItem('access_token');
  const userJson = localStorage.getItem('user');

  if (!token || !userJson) {
    return router.createUrlTree(['/login']);
  }

  try {

    const user = JSON.parse(userJson);

    const role = String(user.role || '')
      .trim()
      .toLowerCase();

    // Manager duoc phep truy cap
    if (role === 'manager') {
      return true;
    }

    // Admin chuyen ve trang Admin
    if (role === 'admin') {
      return router.createUrlTree([
        '/admin/dashboard'
      ]);
    }

    // Employee chuyen ve lich ca nhan
    if (role === 'employee') {
      return router.createUrlTree([
        '/employee/schedule'
      ]);
    }

    return router.createUrlTree(['/login']);

  } catch {

    return router.createUrlTree(['/login']);

  }
};


// ============================================
// DANH SACH ROUTES
// ============================================

export const routes: Routes = [

  // ==========================================
  // 1. DANG NHAP
  // ==========================================

  {
    path: 'login',

    loadComponent: () =>
      import('./features/auth/login/login')
        .then(m => m.Login)
  },


  // ==========================================
  // 2. DANG KY
  // ==========================================

  {
    path: 'register',

    loadComponent: () =>
      import('./features/auth/register/register')
        .then(m => m.Register)
  },


  // ==========================================
  // 3. MANAGER WORKSPACE
  // ==========================================

  {
    path: 'manager',

    canActivate: [managerGuard],

    data: {
      workspace: 'manager'
    },

    loadComponent: () =>
      import(
        './features/workspace/layout/workspace-layout'
      ).then(m => m.WorkspaceLayout),

    children: [

      // ========================================
      // TRANG MAC DINH MANAGER
      // ========================================

      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },


      // ========================================
      // TONG QUAN MANAGER
      // ========================================

      {
        path: 'dashboard',

        loadComponent: () =>
          import(
            './features/manager/dashboard/manager-dashboard'
          ).then(m => m.ManagerDashboard)
      },


      // ========================================
      // US 1.5 - DANH SACH NHAN VIEN
      // ========================================

      {
        path: 'employees',

        loadComponent: () =>
          import(
            './features/manager/employees/employee-list'
          ).then(m => m.EmployeeList)
      },


      // ========================================
      // US 1.5 - THEM HO SO NHAN VIEN
      // ========================================

      {
        path: 'employees/new',

        loadComponent: () =>
          import(
            './features/manager/employees/employee-form'
          ).then(m => m.EmployeeForm)
      },


      // ========================================
      // US 1.6 - CHINH SUA HO SO NHAN VIEN
      // ========================================

      {
        path: 'employees/:id/edit',

        loadComponent: () =>
          import(
            './features/manager/employees/employee-form'
          ).then(m => m.EmployeeForm)
      },


      // ========================================
      // US 2.1 - QUAN LY CA LAM VIEC
      // ========================================

      {
        path: 'shifts',

        loadComponent: () =>
          import('./features/shifts/shifts')
            .then(m => m.Shifts)
      },


      // ========================================
      // US 2.2 - PHAN NHAN VIEN VAO CA
      // ========================================

      {
        path: 'schedule',

        loadComponent: () =>
          import('./features/schedule/schedule')
            .then(m => m.Schedule)
      }

    ]
  },


  // ==========================================
  // 4. EMPLOYEE WORKSPACE
  // ==========================================

  {
    path: 'employee',

    canActivate: [employeeGuard],

    data: {
      workspace: 'employee'
    },

    loadComponent: () =>
      import(
        './features/workspace/layout/workspace-layout'
      ).then(m => m.WorkspaceLayout),

    children: [

      // ========================================
      // TRANG MAC DINH EMPLOYEE
      // ========================================

      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'schedule'
      },


      // ========================================
      // TONG QUAN EMPLOYEE
      // TAM THOI CHUYEN VE LICH LAM VIEC
      // ========================================

      {
        path: 'dashboard',
        pathMatch: 'full',
        redirectTo: 'schedule'
      },


      // ========================================
      // US 2.3 - XEM LICH LAM VIEC CA NHAN
      // ========================================

      {
        path: 'schedule',

        loadComponent: () =>
          import(
            './features/my-schedule/my-schedule'
          ).then(m => m.MySchedule)
      }

    ]
  },


  // ==========================================
  // 5. ADMIN WORKSPACE
  // ==========================================

  {
    path: 'admin',

    canActivate: [adminGuard],

    loadComponent: () =>
      import(
        './features/Admin/layout/admin-layout'
      ).then(m => m.AdminLayout),

    children: [

      // ========================================
      // TRANG MAC DINH ADMIN
      // ========================================

      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },


      // ========================================
      // TONG QUAN ADMIN
      // ========================================

      {
        path: 'dashboard',

        loadComponent: () =>
          import(
            './features/Admin/dashboard/dashboard'
          ).then(m => m.Dashboard)
      },


      // ========================================
      // QUAN LY TAI KHOAN NGUOI DUNG
      // ========================================

      {
        path: 'users',

        loadComponent: () =>
          import(
            './features/Admin/user/users'
          ).then(m => m.Users)
      }

    ]
  },


  // ==========================================
  // 6. CAC ROUTE CU
  // GIU TUONG THICH VOI CODE HIEN TAI
  // ==========================================

  {
    path: 'shifts',
    pathMatch: 'full',
    redirectTo: 'manager/shifts'
  },

  {
    path: 'schedule',
    pathMatch: 'full',
    redirectTo: 'manager/schedule'
  },

  {
    path: 'my-schedule',
    pathMatch: 'full',
    redirectTo: 'employee/schedule'
  },


  // ==========================================
  // 7. TRANG HOME CU
  // ==========================================

  {
    path: 'home',

    canActivate: [authGuard],

    loadComponent: () =>
      import('./features/home/home')
        .then(m => m.Home)
  },


  // ==========================================
  // 8. ROUTE MAC DINH
  // ==========================================

  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  },


  // ==========================================
  // 9. ROUTE KHONG TON TAI
  // ==========================================

  {
    path: '**',
    redirectTo: 'login'
  }

];
