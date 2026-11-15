
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import { EmployeeService } from '../../core/services/employee.service';
import {
  Employee,
  EmployeeRequest
} from '../../shared/models/employee.model';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.css'
})
export class EmployeeList implements OnInit {

  private service = inject(EmployeeService);
  private fb = inject(FormBuilder);

  employees: Employee[] = [];

  showForm = false;
  editingId: string | null = null;

  loading = false;
  errorMessage = '';
  successMessage = '';

  employeeForm = this.fb.group({
    employeeCode: ['', Validators.required],
    fullName: ['', Validators.required],
    email: ['', Validators.email],
    phoneNumber: [
      '',
      [
        Validators.required,
        Validators.pattern(/^(0[0-9]{9}|\+84[0-9]{9})$/)
      ]
    ],
    dateOfBirth: ['', Validators.required],
    position: ['', Validators.required],
    department: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadEmployees();
  }

  // Tải danh sách nhân viên
  loadEmployees(): void {
    this.loading = true;

    this.service.getAll().subscribe({
      next: (data) => {
        this.employees = data;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Không thể tải danh sách nhân viên.';
        this.loading = false;
      }
    });
  }

  // Mở form thêm nhân viên
  openCreate(): void {
    this.editingId = null;
    this.employeeForm.reset();
    this.showForm = true;

    this.errorMessage = '';
    this.successMessage = '';
  }

  // Mở form sửa nhân viên
  openEdit(employee: Employee): void {
    this.editingId = employee.id;

    this.employeeForm.patchValue({
      employeeCode: employee.employeeCode,
      fullName: employee.fullName,
      email: employee.email ?? '',
      phoneNumber: employee.phoneNumber,
      dateOfBirth: employee.dateOfBirth.substring(0, 10),
      position: employee.position,
      department: employee.department
    });

    this.showForm = true;

    this.errorMessage = '';
    this.successMessage = '';
  }

  // Đóng form
  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.employeeForm.reset();
    this.errorMessage = '';
  }

  saveEmployee(): void {
    if (this.loading) return;

    this.errorMessage = '';
    this.successMessage = '';

    if (this.employeeForm.invalid) {
        this.employeeForm.markAllAsTouched();
        return;
    }

    const value = this.employeeForm.getRawValue();

    const data: EmployeeRequest = {
        employeeCode: value.employeeCode!.trim(),
        fullName: value.fullName!.trim(),
        email: value.email?.trim() || null,
        phoneNumber: value.phoneNumber!.trim(),
        dateOfBirth: value.dateOfBirth!,
        position: value.position!.trim(),
        department: value.department!.trim()
    };

    const isEditing = this.editingId !== null;

    this.loading = true;

    const request = isEditing
        ? this.service.update(this.editingId!, data)
        : this.service.create(data);

    request.subscribe({
        next: (savedEmployee) => {
        if (isEditing) {
            this.employees = this.employees.map(employee =>
            employee.id === savedEmployee.id
                ? savedEmployee
                : employee
            );

            this.successMessage = 'Cập nhật nhân viên thành công!';
        } else {
            this.employees = [...this.employees, savedEmployee];

            this.successMessage = 'Thêm nhân viên thành công!';
        }

        this.showForm = false;
        this.editingId = null;
        this.employeeForm.reset();
        this.loading = false;
        },
        error: (error) => {
        this.errorMessage =
            error.error?.message || 'Có lỗi xảy ra khi lưu nhân viên.';

        this.loading = false;
        }
    });
    }

  // Xóa nhân viên
  deleteEmployee(employee: Employee): void {
    if (this.loading) return;

    const confirmed = confirm(
        `Bạn có chắc chắn muốn xóa nhân viên "${employee.fullName}" (${employee.employeeCode}) không?`
    );

    if (!confirmed) return;

    this.errorMessage = '';
    this.successMessage = '';
    this.loading = true;

    this.service.delete(employee.id).subscribe({
        next: () => {
        this.employees = this.employees.filter(
            item => item.id !== employee.id
        );

        this.successMessage = 'Xóa nhân viên thành công!';
        this.loading = false;
        },
        error: (error) => {
        this.errorMessage =
            error.error?.message || 'Không thể xóa nhân viên.';

        this.loading = false;
        }
    });
    }
}
