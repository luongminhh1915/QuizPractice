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

/** /api/account — the caller's own account (Common feature, UC-01 to UC-08). */
export const accountApi = {
  /** Signed-in user, or null for a Guest (204). Also hands out the antiforgery token. */
  me: () => http.get<CurrentUser | ''>('/account/me').then((r) => r.data || null),

  /** UC-01. Answers with the user and the menu of the role. */
  login: (input: LoginInput) => http.post<CurrentUser>('/account/login', input).then((r) => r.data),

  /** UC-01 E2, UC-02 E3, UC-03 E1. The same answer whatever the email. */
  sendVerificationLink: (email: string) => http.post('/account/verification-link', { email }).then(() => undefined),
}
