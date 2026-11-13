import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  inject,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import {
  ShiftAssignmentService,
  EmployeeOption,
  ScheduleCell
} from '../../core/services/shift-assignment.service';

type CalendarView = 'day' | 'week' | 'month';

interface CalendarDay {
  date: string;
  label: string;
  dayNumber: number;
  currentMonth: boolean;
}

@Component({
  selector: 'app-schedule',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './schedule.html',
  styleUrl: './schedule.scss'
})
export class Schedule implements OnInit {

  private service = inject(ShiftAssignmentService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  viewMode: CalendarView = 'week';
  selectedDate = this.toIso(new Date());

  days: CalendarDay[] = [];
  schedule: ScheduleCell[] = [];
  employees: EmployeeOption[] = [];

  loading = false;
  saving = false;
  showAssignForm = false;

  selectedCell: ScheduleCell | null = null;
  selectedEmployeeIds: string[] = [];

  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadEmployees();
    this.refreshCalendar();
  }

  // Chuyen ngay thanh YYYY-MM-DD theo gio dia phuong
  private toIso(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private fromIso(value: string): Date {
    const [y, m, d] = value.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  private addDays(date: Date, amount: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + amount);
    return result;
  }

  // Chon che do Ngay / Tuan / Thang
  setView(mode: CalendarView): void {
    this.viewMode = mode;
    this.refreshCalendar();
  }

  // Doi ngay dang xem
  changeDate(value: string): void {
    if (!value) return;

    this.selectedDate = value;
    this.refreshCalendar();
  }

  // Chuyen den ky truoc hoac ky sau
  navigateDate(direction: number): void {
    const date = this.fromIso(this.selectedDate);

    if (this.viewMode === 'day') {
      date.setDate(date.getDate() + direction);
    } else if (this.viewMode === 'week') {
      date.setDate(date.getDate() + direction * 7);
    } else {
      date.setDate(1);
      date.setMonth(date.getMonth() + direction);
    }

    this.selectedDate = this.toIso(date);
    this.refreshCalendar();
  }

  goToday(): void {
    this.selectedDate = this.toIso(new Date());
    this.refreshCalendar();
  }

  // Tao cac ngay can hien thi
  refreshCalendar(): void {
    const date = this.fromIso(this.selectedDate);

    let start = new Date(date);
    let end = new Date(date);

    if (this.viewMode === 'week') {
      const offset = (date.getDay() + 6) % 7;
      start = this.addDays(date, -offset);
      end = this.addDays(start, 6);
    }

    if (this.viewMode === 'month') {
      const first = new Date(
        date.getFullYear(), date.getMonth(), 1
      );

      const last = new Date(
        date.getFullYear(), date.getMonth() + 1, 0
      );

      const startOffset = (first.getDay() + 6) % 7;
      const endOffset = 6 - ((last.getDay() + 6) % 7);

      start = this.addDays(first, -startOffset);
      end = this.addDays(last, endOffset);
    }

    this.days = [];

    for (
      let current = new Date(start);
      current <= end;
      current = this.addDays(current, 1)
    ) {
      this.days.push({
        date: this.toIso(current),
        label: current.toLocaleDateString('vi-VN', {
          weekday: 'short'
        }),
        dayNumber: current.getDate(),
        currentMonth: current.getMonth() === date.getMonth()
      });
    }

    this.loadSchedule();
  }

  // Lay danh sach nhan vien
  loadEmployees(): void {
    this.service.getEmployees().subscribe({
      next: (data) => {
        this.employees = data;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.errorMessage = this.getError(
          error, 'Không tải được danh sách nhân viên.'
        );
        this.cdr.markForCheck();
      }
    });
  }

  // Lay lich tu Backend
  loadSchedule(): void {
    if (this.days.length === 0) return;

    this.loading = true;
    this.errorMessage = '';

    const from = this.days[0].date;
    const to = this.days[this.days.length - 1].date;

    this.service.getSchedule(from, to).subscribe({
      next: (data) => {
        this.schedule = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = this.getError(
          error, 'Không tải được lịch làm việc.'
        );
        this.cdr.markForCheck();
      }
    });
  }

  // Lay cac ca cua mot ngay
  cellsForDay(date: string): ScheduleCell[] {
    return this.schedule.filter(
      s => s.workDate === date
    );
  }

  // Dinh dang gio
  formatTime(time: string): string {
    return time.slice(0, 5);
  }

  // Nhan vien con thieu de dat muc toi thieu
  missingEmployees(cell: ScheduleCell): number {
    return Math.max(
      0, cell.minEmployees - cell.assignedCount
    );
  }

  // Mo form phan ca
  openAssign(cell: ScheduleCell): void {
    this.selectedCell = cell;
    this.selectedEmployeeIds = [];
    this.errorMessage = '';
    this.successMessage = '';
    this.showAssignForm = true;
  }

  closeAssign(): void {
    this.showAssignForm = false;
    this.selectedCell = null;
    this.selectedEmployeeIds = [];
    this.errorMessage = '';
  }

  // Chi hien thi nhan vien chua duoc phan vao ca
  get availableEmployees(): EmployeeOption[] {
    if (!this.selectedCell) return [];

    const assignedIds = new Set(
      this.selectedCell.employees.map(e => e.id)
    );

    return this.employees.filter(
      e => !assignedIds.has(e.id)
    );
  }

  isSelected(id: string): boolean {
    return this.selectedEmployeeIds.includes(id);
  }

  toggleEmployee(id: string, checked: boolean): void {
    if (checked) {
      if (!this.selectedEmployeeIds.includes(id)) {
        this.selectedEmployeeIds.push(id);
      }
    } else {
      this.selectedEmployeeIds =
        this.selectedEmployeeIds.filter(x => x !== id);
    }
  }

  // Luu phan ca
  saveAssignment(): void {
    const cell = this.selectedCell;

    if (!cell) return;

    this.errorMessage = '';

    if (this.selectedEmployeeIds.length === 0) {
      this.errorMessage = 'Hãy chọn ít nhất một nhân viên.';
      return;
    }

    if (
      cell.assignedCount + this.selectedEmployeeIds.length >
      cell.maxEmployees
    ) {
      this.errorMessage =
        `Ca chỉ cho phép tối đa ${cell.maxEmployees} nhân viên.`;
      return;
    }

    this.saving = true;

    this.service.assignEmployees({
      shiftId: cell.shiftId,
      workDate: cell.workDate,
      employeeIds: this.selectedEmployeeIds
    }).subscribe({
      next: () => {
        this.saving = false;
        this.closeAssign();
        this.successMessage = 'Phân ca thành công!';
        this.loadSchedule();
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.saving = false;
        this.errorMessage = this.getError(
          error, 'Không thể phân nhân viên vào ca.'
        );
        this.cdr.markForCheck();
      }
    });
  }


  // Xoa nhan vien khoi ca lam viec
  removeEmployee(
    cell: ScheduleCell,
    employee: EmployeeOption
  ): void {

    const confirmed = confirm(
      `Bạn có chắc muốn xóa nhân viên "${employee.fullName}" ` +
      `khỏi ca "${cell.shiftName}" ngày ${cell.workDate}?`
    );

    if (!confirmed) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';

    this.service.removeEmployee(
      cell.shiftId,
      cell.workDate,
      employee.id
    ).subscribe({

      next: () => {
        this.successMessage =
          `Đã xóa ${employee.fullName} khỏi ca làm việc.`;

        // Tai lai lich de cap nhat so luong nhan vien
        this.loadSchedule();

        // Cap nhat giao dien Angular
        this.cdr.markForCheck();
      },

      error: (error) => {
        this.errorMessage = this.getError(
          error,
          'Không thể xóa nhân viên khỏi ca làm việc.'
        );

        this.cdr.markForCheck();
      }
    });
  }


  backToShifts(): void {
    this.router.navigate(['/shifts']);
  }

  private getError(error: any, fallback: string): string {
    if (error.status === 401) {
      return 'Phiên đăng nhập đã hết hạn.';
    }

    if (error.status === 403) {
      return 'Bạn không có quyền phân ca làm việc.';
    }

    return error.error?.message || fallback;
  }
}
