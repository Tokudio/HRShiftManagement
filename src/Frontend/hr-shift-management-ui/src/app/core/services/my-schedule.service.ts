import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EmployeeSchedule {
  workDate: string;
  shiftName: string;
  startTime: string;
  endTime: string;
}

@Injectable({
  providedIn: 'root'
})
export class MyScheduleService {
  private http = inject(HttpClient);

  private apiUrl = `${environment.apiBaseUrl}/my-schedule`;

  getMySchedule(
    from: string,
    to: string
  ): Observable<EmployeeSchedule[]> {
    const params = new HttpParams()
      .set('from', from)
      .set('to', to);

    return this.http.get<EmployeeSchedule[]>(
      this.apiUrl,
      { params }
    );
  }
}
