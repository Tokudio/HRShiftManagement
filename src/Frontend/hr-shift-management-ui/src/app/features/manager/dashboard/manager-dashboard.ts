import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';

import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { environment } from '../../../../environments/environment';

import {
  ShiftAssignmentService,
  ScheduleCell
} from '../../../core/services/shift-assignment.service';

interface EmployeeSummary {
  id: string;
  fullName: string;
  status: string;
}

@Component({
  selector: 'app-manager-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './manager-dashboard.html',
  styleUrl: './manager-dashboard.scss'
})
export class ManagerDashboard implements OnInit {

  private http = inject(HttpClient);
  private scheduleService = inject(ShiftAssignmentService);
  private cdr = inject(ChangeDetectorRef);

  name = 'Quản lý';

  employees: EmployeeSummary[] = [];
  todayShifts: ScheduleCell[] = [];

  loading = true;
  errorMessage = '';

  todayLabel = new Date().toLocaleDateString('vi-VN');

  constructor() {
    try {
      const user = JSON.parse(
        localStorage.getItem('user') || '{}'
      );

      this.name = user.fullName || 'Quản lý';
    } catch {
      this.name = 'Quản lý';
    }
  }

  ngOnInit(): void {
    this.loadDashboard();
  }

  // Chuyen ngay thanh YYYY-MM-DD
  private toDateKey(date: Date): string {
    const year = date.getFullYear();

    const month = String(
      date.getMonth() + 1
    ).padStart(2, '0');

    const day = String(
      date.getDate()
    ).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  // Tai du lieu tu Backend
  loadDashboard(): void {

    this.loading = true;
    this.errorMessage = '';

    const today = this.toDateKey(new Date());

    const employeeUrl =
      `${environment.apiBaseUrl}/employee-profiles`;

    forkJoin({
      employees: this.http.get<EmployeeSummary[]>(
        employeeUrl
      ),

      schedule: this.scheduleService.getSchedule(
        today,
        today
      )
    }).subscribe({

      next: ({ employees, schedule }) => {

        this.employees = employees;
        this.todayShifts = schedule;

        this.loading = false;
        this.cdr.markForCheck();
      },

      error: (error) => {
        console.error(
          'Manager dashboard error:',
          error
        );

        this.errorMessage =
          'Không thể tải dữ liệu tổng quan.';

        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  // Tong nhan vien
  get totalEmployees(): number {
    return this.employees.length;
  }

  // Nhan vien dang hoat dong
  get activeEmployees(): number {
    return this.employees.filter(
      e => e.status === 'Active'
    ).length;
  }

  // So ca da co nguoi duoc phan cong
  get assignedShifts(): number {
    return this.todayShifts.filter(
      s => s.assignedCount > 0
    ).length;
  }

  // So ca dang thieu nhan vien
  get missingShifts(): number {
    return this.todayShifts.filter(
      s => s.assignedCount < s.minEmployees
    ).length;
  }

  // Chi hien thi toi da 8 ca
  get displayedShifts(): ScheduleCell[] {
    return this.todayShifts.slice(0, 8);
  }

  // Trang thai nhan su cua ca
  getShiftStatus(shift: ScheduleCell): string {

    if (shift.assignedCount >= shift.minEmployees) {
      return 'Đủ người';
    }

    const missing =
      shift.minEmployees - shift.assignedCount;

    return `Thiếu ${missing}`;
  }

  // Kiem tra ca qua dem
  isOvernight(shift: ScheduleCell): boolean {
    return shift.endTime < shift.startTime;
  }

  // Hien thi gio
  formatTime(time: string): string {
    return time.slice(0, 5);
  }
}
