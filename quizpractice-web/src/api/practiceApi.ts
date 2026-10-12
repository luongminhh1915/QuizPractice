import { http } from './client'
import type { Page } from './types'

export type SelectionType = 'Topic' | 'Dimension'

/** Matches PracticeGroupDto: a topic, or a dimension with its type. */
export interface PracticeGroup {
  id: number
  name: string
  type: string | null
  available: number
}

/** Matches PracticeGroupsDto. total = the shown questions of the whole subject (all groups). */
export interface PracticeGroups {
  total: number
  groups: PracticeGroup[]
}

/** Matches AttemptOutcomeDto (BR-21). */
export interface AttemptOutcome {
  correct: number
  score: number
  passRate: number
  passed: boolean
  durationSeconds: number
}

/** Matches PracticeDetailsDto (UC-21). outcome is empty while the attempt still runs. */
export interface PracticeDetails {
  id: number
  subjectName: string
  mode: 'Practice' | 'Exam'
  title: string
  selectionType: SelectionType | null
  groupName: string | null
  questionCount: number
  status: 'InProgress' | 'Submitted'
  startedAt: string
  outcome: AttemptOutcome | null
}

/** Matches PracticeSummaryDto (UC-19). */
export interface PracticeSummary {
  id: number
  subjectName: string
  mode: 'Practice' | 'Exam'
  title: string
  startedAt: string
  status: 'InProgress' | 'Submitted'
  questionCount: number
  outcome: AttemptOutcome | null
}

export interface PracticeListCriteria {
  subjectId?: number
  mode?: 'Practice' | 'Exam'
  oldestFirst?: boolean
  page: number
  pageSize: number
}

export interface CreatePractice {
  subjectId: number
  questionCount: number
  selectionType: SelectionType
  /** Empty = all topics, or all dimensions. */
  groupId?: number
}

/** /api/practices — Practices List and Practice Details (F#14, F#15). */
export const practiceApi = {
  /** UC-19. */
  list: (criteria: PracticeListCriteria) => http.get<Page<PracticeSummary>>('/practices', { params: criteria }).then((r) => r.data),

  /** UC-19: the subjects the customer has taken something in. */
  filters: () => http.get<{ subjects: { id: number; name: string }[] }>('/practices/filters').then((r) => r.data),

  /** UC-21. 404 when missing or another customer's (E1). */
  get: (id: number) => http.get<PracticeDetails>(`/practices/${id}`).then((r) => r.data),

  /** UC-20 step 2: the subjects the customer may practise; empty is E1. */
  form: () => http.get<{ subjects: { id: number; name: string }[] }>('/practices/form').then((r) => r.data),

  /** UC-20 step 3. */
  groups: (subjectId: number, selectedBy: SelectionType) =>
    http.get<PracticeGroups>('/practices/groups', { params: { subjectId, selectedBy } }).then((r) => r.data),

  /** UC-20: the attempt Quiz Handle opens. 400 QuestionCount when fewer match (E2). */
  create: (practice: CreatePractice) => http.post<{ id: number }>('/practices', practice).then((r) => r.data),
}
