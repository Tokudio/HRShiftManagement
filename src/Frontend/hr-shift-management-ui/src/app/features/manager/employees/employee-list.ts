import { Component, ChangeDetectorRef, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  EmployeeProfile,
  EmployeeProfileService
} from '../../../core/services/employee-profile.service';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.scss'
})
export class EmployeeList implements OnInit {

  private readonly employeeService = inject(EmployeeProfileService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);

  employees: EmployeeProfile[] = [];

  keyword = '';
  selectedDepartment = '';
  selectedStatus = '';

  loading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('created') === '1') {
      this.successMessage = 'Đã thêm hồ sơ nhân viên thành công.';
    }

    this.loadEmployees();
  }

  // LAY DANH SACH NHAN VIEN
  loadEmployees(): void {
    this.loading = true;
    this.errorMessage = '';

    this.employeeService.getAll().subscribe({
      next: employees => {
        this.employees = employees;
        this.loading = false;
        this.cdr.markForCheck();
      },

      error: err => {
        this.errorMessage =
          err.error?.message || 'Không thể tải danh sách nhân viên.';

        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  // DANH SACH PHONG BAN
  get departments(): string[] {
    return [...new Set(
      this.employees.map(x => x.department).filter(Boolean)
    )].sort((a, b) => a.localeCompare(b, 'vi'));
  }

  // TIM KIEM VA LOC
  get filteredEmployees(): EmployeeProfile[] {
    const query = this.keyword.trim().toLocaleLowerCase('vi-VN');

    return this.employees.filter(employee => {
      const text = [
        employee.employeeCode,
        employee.fullName,
        employee.email || ''
      ].join(' ').toLocaleLowerCase('vi-VN');

      return (!query || text.includes(query)) &&
        (!this.selectedDepartment ||
          employee.department === this.selectedDepartment) &&
        (!this.selectedStatus ||
          employee.status === this.selectedStatus);
    });
  }

  // TRANG THAI TIENG VIET
  getStatusLabel(status: string): string {
    switch (status.toLowerCase()) {
      case 'active':
        return 'Đang làm';

      case 'inactive':
        return 'Tạm nghỉ';

      case 'locked':
        return 'Đã khóa';

      default:
        return status || 'Chưa xác định';
    }
  }

  // XOA CAC BO LOC
  clearFilters(): void {
    this.keyword = '';
    this.selectedDepartment = '';
    this.selectedStatus = '';
  }
}
