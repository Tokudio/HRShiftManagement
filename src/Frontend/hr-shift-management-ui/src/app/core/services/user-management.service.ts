
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserListItem {
  id: string;
  fullName: string;
  email: string;
  role: string;
  status: string;
  phoneNumber: string | null;
  lastLoginAt: string | null;
}

// Dữ liệu tạo tài khoản
export interface CreateUserRequest {
  fullName: string;
  email: string;
  phoneNumber: string | null;
  password: string;
  role: string;
}

// Dữ liệu chỉnh sửa tài khoản
export interface UpdateUserRequest {
  fullName: string;
  email: string;
  phoneNumber: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class UserManagementService {
  private http = inject(HttpClient);

  private readonly apiUrl = 'https://localhost:7075/api/Users';

  // 1. Lấy danh sách người dùng
  getUsers(): Observable<UserListItem[]> {
    return this.http.get<UserListItem[]>(this.apiUrl);
  }

  // 2. Tạo tài khoản mới
  createUser(
    request: CreateUserRequest
  ): Observable<UserListItem> {
    return this.http.post<UserListItem>(
      this.apiUrl,
      request
    );
  }

  // 3. Chỉnh sửa thông tin tài khoản
  updateUser(
    userId: string,
    request: UpdateUserRequest
  ): Observable<UserListItem> {
    return this.http.put<UserListItem>(
      `${this.apiUrl}/${userId}`,
      request
    );
  }

  // 4. Cập nhật vai trò
  updateUserRole(
    userId: string,
    role: string
  ): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(
      `${this.apiUrl}/${userId}/role`,
      { role }
    );
  }

  // 5. Khóa hoặc kích hoạt tài khoản
  updateUserStatus(
    userId: string,
    status: string
  ): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(
      `${this.apiUrl}/${userId}/status`,
      { status }
    );
  }
}
