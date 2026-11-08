import { CommonModule } from '@angular/common';
import {
  Component,
  inject,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import {
  Shift,
  ShiftRequest,
  ShiftService
} from '../../core/services/shift.service';

import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-shifts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './shifts.html',
  styleUrl: './shifts.scss'
})
export class Shifts implements OnInit {

  private shiftService = inject(ShiftService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  shifts: Shift[] = [];
  searchText = '';

  loading = false;
  saving = false;
  showForm = false;

  editingId: string | null = null;

  errorMessage = '';
  successMessage = '';

  form: ShiftRequest = this.emptyForm();

  ngOnInit(): void {
    this.loadShifts();
  }

  emptyForm(): ShiftRequest {
    return {
      name: '',
      startTime: '06:00',
      endTime: '14:00',
      minEmployees: 2,
      maxEmployees: 6
    };
  }

  get filteredShifts(): Shift[] {
    const keyword = this.searchText.toLowerCase().trim();

    return this.shifts.filter(s =>
      s.name.toLowerCase().includes(keyword)
    );
  }

  

loadShifts(): void {
  this.loading = true;
  this.errorMessage = '';

  this.shiftService.getAll().subscribe({
    next: (data) => {
      this.shifts = data;
      this.loading = false;

      // Thong bao Angular cap nhat giao dien
      this.cdr.markForCheck();
    },

    error: (error) => {
      this.loading = false;

      this.errorMessage = this.getError(
        error,
        'Không tải được danh sách ca.'
      );

      // Cap nhat giao dien khi API bao loi
      this.cdr.markForCheck();
    }
  });
}

  openCreate(): void {
    this.editingId = null;
    this.form = this.emptyForm();
    this.errorMessage = '';
    this.showForm = true;
  }

  openEdit(shift: Shift): void {
    this.editingId = shift.id;

    this.form = {
      name: shift.name,
      startTime: shift.startTime.slice(0, 5),
      endTime: shift.endTime.slice(0, 5),
      minEmployees: shift.minEmployees,
      maxEmployees: shift.maxEmployees
    };

    this.errorMessage = '';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
  }

  save(): void {
    this.errorMessage = '';

    const data: ShiftRequest = {
      ...this.form,
      name: this.form.name.trim(),
      startTime: this.form.startTime.slice(0, 5) + ':00',
      endTime: this.form.endTime.slice(0, 5) + ':00'
    };

    if (data.name.length < 2) {
      this.errorMessage = 'Tên ca phải có ít nhất 2 ký tự.';
      return;
    }

    if (data.startTime === data.endTime) {
      this.errorMessage = 'Giờ bắt đầu phải khác giờ kết thúc.';
      return;
    }

    if (
      !Number.isInteger(data.minEmployees) ||
      !Number.isInteger(data.maxEmployees) ||
      data.minEmployees < 1 ||
      data.maxEmployees < data.minEmployees
    ) {
      this.errorMessage = 'Số lượng nhân viên không hợp lệ.';
      return;
    }

    this.saving = true;
    const isEditing = this.editingId !== null;

    const request = this.editingId
      ? this.shiftService.update(this.editingId, data)
      : this.shiftService.create(data);

    request.subscribe({
      next: () => {
        this.saving = false;
        this.closeForm();
        this.successMessage = isEditing
          ? 'Cập nhật ca thành công!'
          : 'Thêm ca làm việc thành công!';
        this.loadShifts();
      },
      error: (error) => {
        this.saving = false;
        this.errorMessage = this.getError(
          error, 'Không thể lưu ca làm việc.'
        );
      }
    });
  }

  deleteShift(shift: Shift): void {
    if (!shift.canDelete) {
      this.errorMessage =
        'Không thể xóa ca đã có lịch phân công.';
      return;
    }

    if (!confirm(`Bạn có chắc muốn xóa "${shift.name}"?`)) {
      return;
    }

    this.shiftService.delete(shift.id).subscribe({
      next: () => {
        this.successMessage = 'Xóa ca thành công!';
        this.loadShifts();
      },
      error: (error) => {
        this.errorMessage = this.getError(
          error, 'Không thể xóa ca làm việc.'
        );
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }

  private getError(error: any, fallback: string): string {
    if (error.status === 401) {
      return 'Phiên đăng nhập không hợp lệ. Hãy đăng nhập lại.';
    }
    if (error.status === 403) {
      return 'Tài khoản không có quyền quản lý ca.';
    }
    return error.error?.message || fallback;
  }
}
