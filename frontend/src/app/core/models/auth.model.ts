export interface AuthUserProfile {
  id: number;
  name: string;
  email: string;
  role: string;
  roleCode: string;
  department: string;
  employeeId: number;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresIn: number;
  user: AuthUserProfile;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  password: string;
}

export interface MessageResponse {
  message: string;
}

export interface UpdateMyProfileRequest {
  name: string;
  email: string;
  department: string;
}
