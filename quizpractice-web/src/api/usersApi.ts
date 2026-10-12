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

/** Matches UserDto of the API: User Details. */
export interface UserDetails extends UserSummary {
  avatar: string | null
  address: string | null
}

/**
 * After a save. passwordSent: null when no password was due, false when it could not be
 * emailed (UC-63 E2). releasedSubjects: subjects that lost their owner (A3).
 */
export interface UserSaved {
  user: UserDetails
  passwordSent: boolean | null
  releasedSubjects: string[]
}

/** A new account (UC-63 step 3). */
export interface NewUserInput {
  avatar?: File
  fullName: string
  gender: Gender
  email: string
  mobile: string
  address?: string
  roleId: number
}

/** A stored account: only the role and the status change (BR-20). */
export interface UserEditInput {
  roleId: number
  status: UserStatus
  confirmHandOver?: boolean
}

function newUserForm(input: NewUserInput): FormData {
  const data = new FormData()
  data.append('fullName', input.fullName)
  data.append('gender', input.gender)
  data.append('email', input.email)
  data.append('mobile', input.mobile)
  data.append('address', input.address ?? '')
  data.append('roleId', String(input.roleId))
  if (input.avatar) data.append('avatar', input.avatar)
  return data
}

/** /api/users — UC-62 View Users List, UC-63 Add or Edit User (Admin only). */
export const usersApi = {
  list: (criteria: UserCriteria) => http.get<Page<UserSummary>>('/users', { params: criteria }).then((r) => r.data),

  choices: () => http.get<UserChoices>('/users/choices').then((r) => r.data),

  get: (id: number) => http.get<UserDetails>(`/users/${id}`).then((r) => r.data),

  /** 409 User.EmailTaken: the email has an account already (E1). */
  create: (input: NewUserInput) => http.post<UserSaved>('/users', newUserForm(input)).then((r) => r.data),

  /** 409 User.Self (E3), or User.OwnsSubjects until confirmHandOver (A3). */
  update: (id: number, input: UserEditInput) => http.put<UserSaved>(`/users/${id}`, input).then((r) => r.data),
}
