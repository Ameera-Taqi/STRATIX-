import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthUserProfile,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResponse,
  MessageResponse,
  RefreshTokenRequest,
  RegisterOrganizationRequest,
  ResetPasswordRequest,
  UpdateMyProfileRequest,
} from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  login(body: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/auth/login`, body);
  }

  registerOrganization(body: RegisterOrganizationRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/auth/register`, body);
  }

  refresh(body: RefreshTokenRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/auth/refresh`, body);
  }

  logout(body: RefreshTokenRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/auth/logout`, body);
  }

  forgotPassword(body: ForgotPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.base}/auth/forgot-password`, body);
  }

  resetPassword(body: ResetPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.base}/auth/reset-password`, body);
  }

  me(): Observable<AuthUserProfile> {
    return this.http.get<AuthUserProfile>(`${this.base}/auth/me`);
  }

  updateMyProfile(body: UpdateMyProfileRequest): Observable<AuthUserProfile> {
    return this.http.patch<AuthUserProfile>(`${this.base}/auth/me`, body);
  }
}
