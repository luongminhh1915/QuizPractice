import { http } from './client'
import type { ListCriteria, Page } from './types'

export type Gender = 'Male' | 'Female' | 'Other'

/** User (BR-01, BR-23): Unverified until the verification link is opened, or until the Admin sets it Active. */
export type UserStatus = 'Unverified' | 'Active' | 'Inactive'

/** A user role setting. */
export interface RoleLookup {
  id: number
  name: string
}

/** Matches UserSummaryDto of the API: a row of Users List. */
export interface UserSummary {
  id: number
  fullName: string
  gender: Gender
  email: string
  mobile: string
  roleId: number
  role: string
  status: UserStatus
}

export interface UserChoices {
  roles: RoleLookup[]
}

export interface UserCriteria extends ListCriteria {
  gender?: Gender
  roleId?: number
  status?: UserStatus
}

/** /api/users — UC-62 View Users List, UC-63 Add or Edit User (Admin only). */
export const usersApi = {
  list: (criteria: UserCriteria) => http.get<Page<UserSummary>>('/users', { params: criteria }).then((r) => r.data),

  choices: () => http.get<UserChoices>('/users/choices').then((r) => r.data),
}
