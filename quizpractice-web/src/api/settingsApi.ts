import { http } from './client'
import type { ActiveStatus, ListCriteria, Page } from './types'

/** The eight fixed setting types (D24), as the API names them. */
export type SettingType =
  | 'UserRole'
  | 'SystemMenu'
  | 'PostCategory'
  | 'SubjectCategory'
  | 'TestType'
  | 'QuestionLevel'
  | 'LessonType'
  | 'DimensionType'

/** How the screens name each type (SRS III 9.4). */
export const SETTING_TYPE_LABELS: Record<SettingType, string> = {
  UserRole: 'User Roles',
  SystemMenu: 'System Menu',
  PostCategory: 'Post Categories',
  SubjectCategory: 'Subject Categories',
  TestType: 'Test Types',
  QuestionLevel: 'Question Levels',
  LessonType: 'Lesson Types',
  DimensionType: 'Subject Dimension',
}

/** Matches SettingSummaryDto of the API: a row of Settings List. */
export interface SettingSummary {
  id: number
  type: SettingType
  value: string
  displayOrder: number
  status: ActiveStatus
  /** Fingerprint the row was loaded with (E2). */
  version: string
  /** False for a value the system needs: it stays Active. */
  canDeactivate: boolean
}

export interface SettingCriteria extends ListCriteria {
  type?: SettingType
  status?: ActiveStatus
}

/** Matches SettingDto of the API: Setting Details (UC-65). */
export interface Setting extends SettingSummary {
  description: string | null
  /** Fingerprint the form was loaded with (E2). */
  version: string
  /** The system reads this type by value: the value cannot change. */
  valueLocked: boolean
  canDeactivate: boolean
  /** System Menu: the roles that see the item (A4). */
  menuRoles: string[]
}

export interface SettingSaved {
  setting: Setting
  /** Quizzes that now draw by the renamed dimension type. */
  quizzesFollowed: number
}

/** Setting Details form; type only for a new setting. */
export interface SettingInput {
  type?: SettingType
  value: string
  displayOrder?: number | null
  description?: string
  status: ActiveStatus
}

/** /api/settings — UC-64 to UC-66 (Admin only). */
export const settingsApi = {
  list: (criteria: SettingCriteria) => http.get<Page<SettingSummary>>('/settings', { params: criteria }).then((r) => r.data),

  get: (id: number) => http.get<Setting>(`/settings/${id}`).then((r) => r.data),

  create: (input: SettingInput) => http.post<SettingSaved>('/settings', input).then((r) => r.data),

  /** 409 Setting.Changed (E2), Setting.Required, or Setting.InUse until confirmInUse (UC-66 E1). */
  update: (id: number, input: SettingInput & { version: string; confirmInUse?: boolean }) =>
    http.put<SettingSaved>(`/settings/${id}`, input).then((r) => r.data),

  /** UC-66: 409 Setting.Changed (E2), Setting.Required, or Setting.InUse until confirmInUse (E1). */
  changeStatus: (id: number, status: ActiveStatus, version: string, confirmInUse = false) =>
    http.put<SettingSummary>(`/settings/${id}/status`, { status, version, confirmInUse }).then((r) => r.data),
}
