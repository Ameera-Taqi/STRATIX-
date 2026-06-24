import { Injectable, signal } from '@angular/core';
import { UserRole } from '../models/user.model';

export interface CurrentUserProfile {
  id: number;
  name: string;
  email: string;
  role: string;
  roleCode: UserRole;
  department: string;
  employeeId: number;
}

const DEFAULT_PROFILE: CurrentUserProfile = {
  id: 1,
  name: 'Admin',
  email: 'admin@stratix.local',
  role: 'Admin',
  roleCode: 'ADMIN',
  department: 'IT',
  employeeId: 1,
};
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly _profile = signal<CurrentUserProfile>({ ...DEFAULT_PROFILE });

  readonly profile = this._profile.asReadonly();

  updateProfile(patch: Partial<Pick<CurrentUserProfile, 'name' | 'email' | 'department'>>): void {
    this._profile.update((current) => ({ ...current, ...patch }));
  }

  setProfile(profile: CurrentUserProfile): void {
    this._profile.set({ ...profile });
  }

  resetProfile(): void {
    this._profile.set({ ...DEFAULT_PROFILE });
  }
}
