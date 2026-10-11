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

/** /api/account — the caller's own account (Common feature, UC-01 to UC-08). */
export const accountApi = {
  /** Signed-in user, or null for a Guest (204). Also hands out the antiforgery token. */
  me: () => http.get<CurrentUser | ''>('/account/me').then((r) => r.data || null),
}
