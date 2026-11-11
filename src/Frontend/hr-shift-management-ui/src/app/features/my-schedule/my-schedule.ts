import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { interval, Subscription } from 'rxjs';

import {
  EmployeeSchedule,
  MyScheduleService
} from '../../core/services/my-schedule.service';

import { AuthService } from '../../core/services/auth.service';

type ViewMode = 'day' | 'week';

@Component({
  selector: 'app-my-schedule',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './my-schedule.html',
  styleUrl: './my-schedule.scss'
})
export class MySchedule implements OnInit, OnDestroy {
  private scheduleService = inject(MyScheduleService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  private refreshSubscription?: Subscription;
  private requestId = 0;

  viewMode: ViewMode = 'week';
  selectedDate = this.toIso(new Date());

  shifts: EmployeeSchedule[] = [];
  loading = false;
  errorMessage = '';
  lastUpdated = '';

  ngOnInit(): void {
    this.loadSchedule();

    // Tu cap nhat moi 30 giay khi trang dang mo
    this.refreshSubscription = interval(30000).subscribe(() => {
      this.loadSchedule(false);
    });
  }

  ngOnDestroy(): void {
    this.refreshSubscription?.unsubscribe();
    this.requestId++;
  }

  private toIso(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private fromIso(value: string): Date {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  private getRange(): { from: string; to: string } {
    if (this.viewMode === 'day') {
      return {
        from: this.selectedDate,
        to: this.selectedDate
      };
    }

    const selected = this.fromIso(this.selectedDate);

    // Tuan bat dau tu Thu Hai
    const offset = (selected.getDay() + 6) % 7;

    const monday = new Date(selected);
    monday.setDate(selected.getDate() - offset);

    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);

    return {
      from: this.toIso(monday),
      to: this.toIso(sunday)
    };
  }

  get periodLabel(): string {
    const { from, to } = this.getRange();

    return this.viewMode === 'day'
      ? this.formatDate(from)
      : `${this.formatDate(from)} - ${this.formatDate(to)}`;
  }

  setView(mode: ViewMode): void {
    if (this.viewMode === mode) return;

    this.viewMode = mode;
    this.loadSchedule();
  }

  changeDate(value: string): void {
    if (!value) return;

    this.selectedDate = value;
    this.loadSchedule();
  }

  navigateDate(direction: number): void {
    const date = this.fromIso(this.selectedDate);
    const numberOfDays = this.viewMode === 'week' ? 7 : 1;

    date.setDate(date.getDate() + direction * numberOfDays);

    this.selectedDate = this.toIso(date);
    this.loadSchedule();
  }

  goToday(): void {
    this.selectedDate = this.toIso(new Date());
    this.loadSchedule();
  }

  loadSchedule(showLoading = true): void {
    const { from, to } = this.getRange();
    const currentId = ++this.requestId;

    if (showLoading) {
      this.loading = true;
    }

    this.errorMessage = '';

    this.scheduleService.getMySchedule(from, to).subscribe({
      next: (data) => {
        if (currentId !== this.requestId) return;

        this.shifts = data;
        this.loading = false;
        this.lastUpdated = new Date().toLocaleTimeString('vi-VN');

        this.cdr.markForCheck();
      },
      error: (error) => {
        if (currentId !== this.requestId) return;

        this.loading = false;

        if (error.status === 401) {
          this.errorMessage =
            'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
        } else if (error.status === 403) {
          this.errorMessage =
            'Bạn không có quyền xem lịch làm việc này.';
        } else {
          this.errorMessage =
            error.error?.message || 'Không thể tải lịch làm việc.';
        }

        this.cdr.markForCheck();
      }
    });
  }

  formatDate(value: string): string {
    const [year, month, day] = value.split('-');
    return `${day}/${month}/${year}`;
  }

  formatTime(value: string): string {
    return value.slice(0, 5);
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }
}
