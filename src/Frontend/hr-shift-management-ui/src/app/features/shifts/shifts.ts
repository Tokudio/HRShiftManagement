import { CommonModule } from '@angular/common';
import {
  Component,
  inject,
  OnInit,
  OnDestroy,
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
export class Shifts implements OnInit, OnDestroy {

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

  // Quan ly thong bao Popup
  private _errorMessage = '';
  private _successMessage = '';

  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  get errorMessage(): string {
    return this._errorMessage;
  }

  set errorMessage(message: string) {
    this._errorMessage = message;

    if (message) {
      this._successMessage = '';
      this.showToast(message, 'error');
    }
  }

  get successMessage(): string {
    return this._successMessage;
  }

  set successMessage(message: string) {
    this._successMessage = message;

    if (message) {
      this._errorMessage = '';
      this.showToast(message, 'success');
    }
  }

  // Hien thi Popup
  private showToast(
    message: string,
    type: 'success' | 'error'
  ): void {
    if (this.toastTimer !== null) {
      clearTimeout(this.toastTimer);
    }

    this.toastMessage = message;
    this.toastType = type;

    this.cdr.markForCheck();

    // Tu dong an sau 5 giay
    this.toastTimer = setTimeout(() => {
      this.toastMessage = '';
      this.toastTimer = null;
      this.cdr.markForCheck();
    }, 5000);
  }

  // Dong Popup thu cong
  closeToast(): void {
    if (this.toastTimer !== null) {
      clearTimeout(this.toastTimer);
      this.toastTimer = null;
    }

    this.toastMessage = '';
    this.cdr.markForCheck();
  }

  ngOnDestroy(): void {
    if (this.toastTimer !== null) {
      clearTimeout(this.toastTimer);
    }
  }

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
    this.successMessage = '';

    const data: ShiftRequest = {
      ...this.form,
      name: this.form.name.trim(),
      startTime: this.form.startTime.slice(0, 5) + ':00',
      endTime: this.form.endTime.slice(0, 5) + ':00'
    };

    if (data.name.length < 2) {
      this.errorMessage =
        'Tên ca phải có ít nhất 2 ký tự.';
      return;
    }

    // Chuyen gio thanh so phut
    const startParts = data.startTime.split(':').map(Number);
    const endParts = data.endTime.split(':').map(Number);

    const startMinutes = startParts[0] * 60 + startParts[1];
    const endMinutes = endParts[0] * 60 + endParts[1];

    if (startMinutes === endMinutes) {
      this.errorMessage =
        'Giờ bắt đầu không được trùng giờ kết thúc.';
      return;
    }

    // Kiem tra ca qua dem
    const isOvernight = endMinutes < startMinutes;

    // Tinh thoi luong ca (phut)
    const durationMinutes =
      (endMinutes - startMinutes + 1440) % 1440;

    // Kiem tra thoi luong
    if (durationMinutes < 30 || durationMinutes > 720) {
      this.errorMessage =
        'Thời lượng ca phải từ 30 phút đến 12 giờ.';
      return;
    }

    if (
      !Number.isInteger(data.minEmployees) ||
      !Number.isInteger(data.maxEmployees) ||
      data.minEmployees < 1 ||
      data.maxEmployees < data.minEmployees ||
      data.maxEmployees > 10000
    ) {
      this.errorMessage =
        'Số lượng nhân viên không hợp lệ.';
      return;
    }

    // Xac nhan khi ca qua dem
    if (isOvernight) {
      const confirmed = window.confirm(
        'Giờ kết thúc nhỏ hơn giờ bắt đầu. ' +
        'Bạn có muốn thiết lập ca làm việc qua đêm không?'
      );

      if (!confirmed) {
        this.errorMessage =
          'Chưa lưu ca. Vui lòng nhập lại thời gian.';
        return;
      }
    }

    // Gui trang thai xac nhan len Backend
    data.confirmOvernight = isOvernight;

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
        this.cdr.markForCheck();
      },

      error: (error) => {
        this.saving = false;

        this.errorMessage = this.getError(
          error,
          'Không thể lưu ca làm việc.'
        );

        this.cdr.markForCheck();
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

  isOvernight(
    startTime: string,
    endTime: string
  ): boolean {
    return endTime.slice(0, 5) < startTime.slice(0, 5);
  }

    // Chuyen sang trang lich phan ca - US 2.2
  goSchedule(): void {
    this.router.navigate(['/schedule']);
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
