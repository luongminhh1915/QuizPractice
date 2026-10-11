import { http } from './client'

/** Matches CurrentUserDto of the API. */
export interface CurrentUser {
  id: number
  email: string
  fullName: string
  role: string
  avatar: string | null
  /** System Menu items the role may open, in display order (D23). */
  menu: string[]
}

export interface LoginInput {
  email: string
  password: string
}

export type Gender = 'Male' | 'Female' | 'Other'

/** User Register form (UC-02). */
export interface RegisterInput {
  fullName: string
  gender: Gender
  email: string
  mobile: string
}

/** Matches VerifyEmailDto of the API (UC-03). */
export interface VerifyEmailResult {
  email: string
  alreadyVerified: boolean
  passwordSent: boolean
}

/** New password form (UC-05, UC-07). */
export interface NewPasswordInput {
  newPassword: string
  confirmPassword: string
}

/** Change Password form (UC-07). */
export interface ChangePasswordInput extends NewPasswordInput {
  currentPassword: string
}

/** Matches ProfileDto of the API (UC-06). */
export interface Profile {
  email: string
  fullName: string
  gender: Gender
  mobile: string
  avatar: string | null
}

/** User Profile form (UC-06). avatar is a new picture; removeAvatar clears the stored one. */
export interface ProfileInput {
  fullName: string
  gender: Gender
  mobile: string
  avatar?: File
  removeAvatar: boolean
}

function profileFormData(input: ProfileInput): FormData {
  const data = new FormData()
  data.append('fullName', input.fullName)
  data.append('gender', input.gender)
  data.append('mobile', input.mobile)
  if (input.avatar) data.append('avatar', input.avatar)
  if (input.removeAvatar) data.append('removeAvatar', 'true')
  return data
}

/** /api/account — the caller's own account (Common feature, UC-01 to UC-08). */
export const accountApi = {
  /** Signed-in user, or null for a Guest (204). Also hands out the antiforgery token. */
  me: () => http.get<CurrentUser | ''>('/account/me').then((r) => r.data || null),

  /** UC-01. Answers with the user and the menu of the role. */
  login: (input: LoginInput) => http.post<CurrentUser>('/account/login', input).then((r) => r.data),

  /** UC-02. Answers with the email the verification link went to. */
  register: (input: RegisterInput) => http.post<{ email: string }>('/account/register', input).then((r) => r.data),

  /** UC-03. 400 Link.Invalid when the link is not genuine or has expired (E1). */
  verifyEmail: (token: string) => http.post<VerifyEmailResult>('/account/verify', { token }).then((r) => r.data),

  /** UC-04. The same answer whatever the email; 502 when the mail could not be sent (E1). */
  sendPasswordResetLink: (email: string) =>
    http.post('/account/password-reset-link', { email }).then(() => undefined),

  /** UC-05 step 2. 400 Link.Invalid when the link is not genuine, has expired or was used (E1). */
  checkResetLink: (token: string) => http.post('/account/password-reset/check', { token }).then(() => true),

  /** UC-05. */
  resetPassword: (token: string, input: NewPasswordInput) =>
    http.post('/account/password-reset', { token, ...input }).then(() => undefined),

  /** UC-06. */
  profile: () => http.get<Profile>('/account/profile').then((r) => r.data),

  /** UC-06. Answers with the stored profile. */
  saveProfile: (input: ProfileInput) => http.put<Profile>('/account/profile', profileFormData(input)).then((r) => r.data),

  /** UC-07. 400 with the currentPassword field when it is wrong (E1). */
  changePassword: (input: ChangePasswordInput) => http.put('/account/password', input).then(() => undefined),

  /** UC-08. Also succeeds when the session has already expired (A1). */
  logout: () => http.post('/account/logout').then(() => undefined),

  /** UC-01 E2, UC-02 E3, UC-03 E1. The same answer whatever the email. */
  sendVerificationLink: (email: string) => http.post('/account/verification-link', { email }).then(() => undefined),
}
