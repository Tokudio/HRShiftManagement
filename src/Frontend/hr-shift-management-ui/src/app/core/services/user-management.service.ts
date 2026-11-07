
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

@Injectable({
  providedIn: 'root',
})
export class UserManagementService {
  private http = inject(HttpClient);

  private readonly apiUrl = 'https://localhost:7075/api/Users';

  getUsers(): Observable<UserListItem[]> {
    return this.http.get<UserListItem[]>(this.apiUrl);
  }
  updateUserRole(
  userId: string,
  role: string
): Observable<{ message: string }> {
  return this.http.put<{ message: string }>(
    `${this.apiUrl}/${userId}/role`,
    { role }
  );
}
}


