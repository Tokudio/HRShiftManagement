import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize, map, of, tap } from 'rxjs';

export interface RegisterRequest {
    fullName: string;
    email: string;
    password: string;
    confirmPassword: string;
    phoneNumber: string | null;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface AuthUser {
    id: string;
    fullName: string;
    email: string;
    role: string;
}

export interface LoginResponse {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    user: AuthUser;
}

@Injectable({
    providedIn: 'root',
})
export class AuthService {
    private http = inject(HttpClient);

    private readonly apiUrl = 'https://localhost:7075/api/Auth';

    register(data: RegisterRequest): Observable<LoginResponse> {
        return this.http.post<LoginResponse>(`${this.apiUrl}/register`, data);
    }

    login(data: LoginRequest): Observable<LoginResponse> {
        return this.http
            .post<LoginResponse>(`${this.apiUrl}/login`, data)
            .pipe(
                tap((response) => {
                    localStorage.setItem('access_token', response.accessToken);
                    localStorage.setItem('refresh_token', response.refreshToken);
                    localStorage.setItem('user', JSON.stringify(response.user));
                }),
            );
    }

    logout(): Observable<void> {
        const refreshToken = localStorage.getItem('refresh_token');

        if (!refreshToken) {
            this.clearSession();
            return of(undefined);
        }

        return this.http
            .post<{ message: string }>(
                `${this.apiUrl}/revoke-token`,
                { refreshToken }
            )
            .pipe(
                map(() => undefined),
                finalize(() => this.clearSession())
            );
    }

    private clearSession(): void {
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('user');
    }
}
