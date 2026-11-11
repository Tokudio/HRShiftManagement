import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Shift {
  id: string;
  name: string;
  startTime: string;
  endTime: string;
  minEmployees: number;
  maxEmployees: number;
  canDelete: boolean;
}

export interface ShiftRequest {
  name: string;
  startTime: string;
  endTime: string;
  minEmployees: number;
  maxEmployees: number;
}

@Injectable({
  providedIn: 'root'
})
export class ShiftService {
  private http = inject(HttpClient);

  private apiUrl = `${environment.apiBaseUrl}/shifts`;

  // Lay danh sach ca lam viec
  getAll(): Observable<Shift[]> {
    return this.http.get<Shift[]>(this.apiUrl);
  }

  // Lay chi tiet mot ca
  getById(id: string): Observable<Shift> {
    return this.http.get<Shift>(
      `${this.apiUrl}/${id}`
    );
  }

  // Tao ca lam viec moi
  create(data: ShiftRequest): Observable<Shift> {
    return this.http.post<Shift>(
      this.apiUrl, data
    );
  }

  // Chinh sua ca lam viec
  update(
    id: string,
    data: ShiftRequest
  ): Observable<Shift> {
    return this.http.put<Shift>(
      `${this.apiUrl}/${id}`, data
    );
  }

  // Xoa ca lam viec
  delete(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}
