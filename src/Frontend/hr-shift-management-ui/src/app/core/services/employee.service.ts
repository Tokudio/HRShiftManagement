
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Employee,
  EmployeeRequest
} from '../../shared/models/employee.model';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {

  private http = inject(HttpClient);

  private apiUrl = `${environment.apiBaseUrl}/employees`;

  getAll(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.apiUrl);
  }

  getById(id: string): Observable<Employee> {
    return this.http.get<Employee>(`${this.apiUrl}/${id}`);
  }

  create(data: EmployeeRequest): Observable<Employee> {
    return this.http.post<Employee>(this.apiUrl, data);
  }

  update(id: string, data: EmployeeRequest): Observable<Employee> {
    return this.http.put<Employee>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
  return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
