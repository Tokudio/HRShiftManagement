import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';

// ============================================
// THONG TIN HO SO NHAN VIEN
// ============================================

export interface EmployeeProfile {
  id: string;
  userId: string | null;
  employeeCode: string;
  fullName: string;
  phoneNumber: string;
  dateOfBirth: string | null;
  position: string;
  department: string;
  email: string | null;
  address: string | null;
  joinedOn: string | null;
  notes: string | null;
  status: string;
  isLinkedToAccount: boolean;
  isProfileComplete: boolean;
}

// ============================================
// DU LIEU THEM / SUA HO SO
// ============================================

export interface EmployeeProfileRequest {
  fullName: string;
  phoneNumber: string;
  dateOfBirth: string;
  position: string;
  department: string;
  email: string | null;
  address: string | null;
  joinedOn: string | null;
  notes: string | null;
}

// ============================================
// RESPONSE KHI CAP NHAT HO SO
// ============================================

export interface UpdateEmployeeResponse {
  message: string;
  data: EmployeeProfile;
}

// ============================================
// EMPLOYEE PROFILE SERVICE
// ============================================

@Injectable({
  providedIn: 'root'
})
export class EmployeeProfileService {

  private http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiBaseUrl}/employee-profiles`;

  // US 1.5 - Lay danh sach nhan vien
  getAll(): Observable<EmployeeProfile[]> {
    return this.http.get<EmployeeProfile[]>(
      this.apiUrl
    );
  }

  // US 1.5 - Xem chi tiet ho so
  getById(id: string): Observable<EmployeeProfile> {
    return this.http.get<EmployeeProfile>(
      `${this.apiUrl}/${id}`
    );
  }

  // US 1.5 - Them ho so nhan vien
  create(
    data: EmployeeProfileRequest
  ): Observable<EmployeeProfile> {

    return this.http.post<EmployeeProfile>(
      this.apiUrl,
      data
    );
  }

  // US 1.6 - Cap nhat thong tin nhan vien
  update(
    id: string,
    data: EmployeeProfileRequest
  ): Observable<EmployeeProfile> {

    return this.http.put<UpdateEmployeeResponse>(
      `${this.apiUrl}/${id}`,
      data
    ).pipe(
      map(response => response.data)
    );
  }
}
