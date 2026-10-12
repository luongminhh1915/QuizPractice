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
}

export interface SettingCriteria extends ListCriteria {
  type?: SettingType
  status?: ActiveStatus
}

/** /api/settings — UC-64 to UC-66 (Admin only). */
export const settingsApi = {
  list: (criteria: SettingCriteria) => http.get<Page<SettingSummary>>('/settings', { params: criteria }).then((r) => r.data),
}
