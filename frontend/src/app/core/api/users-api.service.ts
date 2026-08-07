import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateUserRequest,
  UpdateUserRequest,
  User,
  UserDirectoryItem,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  /** Full user administration list (OrgAdmins). Prefer getDirectoryUsers for pickers. */
  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.base}/users`);
  }

  /** Active tenant directory for assignee/manager pickers (all tenant roles). */
  getDirectoryUsers(): Observable<UserDirectoryItem[]> {
    return this.http.get<UserDirectoryItem[]>(`${this.base}/directory/users`);
  }

  getUser(id: number): Observable<User> {
    return this.http.get<User>(`${this.base}/users/${id}`);
  }

  createUser(body: CreateUserRequest): Observable<User> {
    return this.http.post<User>(`${this.base}/users`, body);
  }

  updateUser(id: number, body: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${this.base}/users/${id}`, body);
  }
}
