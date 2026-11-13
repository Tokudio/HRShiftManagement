import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// Thong tin nhan vien
export interface EmployeeOption {
  id: string;
  fullName: string;
  email: string;
}

// Thong tin mot ca trong lich lam viec
export interface ScheduleCell {
  workDate: string;
  shiftId: string;
  shiftName: string;
  startTime: string;
  endTime: string;
  minEmployees: number;
  maxEmployees: number;
  assignedCount: number;
  employees: EmployeeOption[];
}

// Du lieu gui len khi phan ca
export interface AssignEmployeesRequest {
  shiftId: string;
  workDate: string;
  employeeIds: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ShiftAssignmentService {

  private http = inject(HttpClient);

  private apiUrl =
    `${environment.apiBaseUrl}/shift-assignments`;

  // 1. Lay danh sach nhan vien
  getEmployees(): Observable<EmployeeOption[]> {
    return this.http.get<EmployeeOption[]>(
      `${this.apiUrl}/employees`
    );
  }

  // 2. Lay lich lam viec theo khoang ngay
  getSchedule(
    from: string,
    to: string
  ): Observable<ScheduleCell[]> {

    const params = new HttpParams()
      .set('from', from)
      .set('to', to);

    return this.http.get<ScheduleCell[]>(
      `${this.apiUrl}/schedule`,
      { params }
    );
  }

  // 3. Phan mot hoac nhieu nhan vien vao ca
  assignEmployees(
    data: AssignEmployeesRequest
  ): Observable<{ message: string }> {

    return this.http.post<{ message: string }>(
      this.apiUrl,
      data
    );
  }

  // Huy phan cong mot nhan vien khoi ca
  removeEmployee(
    shiftId: string,
    workDate: string,
    employeeId: string
  ): Observable<void> {

    const params = new HttpParams()
      .set('workDate', workDate);

    return this.http.delete<void>(
      `${this.apiUrl}/${shiftId}/employees/${employeeId}`,
      { params }
    );
  }

}
