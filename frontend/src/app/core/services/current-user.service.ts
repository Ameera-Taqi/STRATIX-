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

@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly _profile = signal<CurrentUserProfile | null>(null);

  readonly profile = this._profile.asReadonly();

  updateProfile(patch: Partial<Pick<CurrentUserProfile, 'name' | 'email' | 'department'>>): void {
    this._profile.update((current) => (current ? { ...current, ...patch } : current));
  }

  setProfile(profile: CurrentUserProfile): void {
    this._profile.set({ ...profile });
  }

  clearProfile(): void {
    this._profile.set(null);
  }

  /** @deprecated Use clearProfile — no default Admin persona. */
  resetProfile(): void {
    this.clearProfile();
  }
}
