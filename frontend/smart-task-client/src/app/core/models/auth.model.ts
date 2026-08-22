export type UserRole = 'Admin' | 'ProjectManager' | 'TeamMember';

export interface UserResponse {
  readonly userId: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly roles: readonly UserRole[];
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface RegisterRequest {
  readonly email: string;
  readonly password: string;
  readonly firstName: string;
  readonly lastName: string;
}

export interface AuthenticationResponse {
  readonly accessToken: string;
  readonly accessTokenExpiresAtUtc: string;
  readonly user: UserResponse;
}
