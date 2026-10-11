import { http } from './client'
import type { ActiveStatus, ListCriteria, Page } from './types'
import type { QuestionLookup } from './questionsApi'

/** Matches QuizSummaryDto of the API: a row of Quizzes List. */
export interface QuizSummary {
  id: number
  name: string
  subjectId: number
  subject: string
  levelId: number
  level: string
  questionCount: number
  durationMin: number
  passRate: number
  typeId: number
  /** Test Type setting: Simulation or Lesson quiz. */
  type: string
  status: ActiveStatus
  /** An attempt exists: no Edit and no Delete any more (BR-13). */
  isTaken: boolean
}

/** Choices of the filters: subjects the user may reach (BR-11) and the quiz types. */
export interface QuizFilters {
  subjects: QuestionLookup[]
  types: QuestionLookup[]
}

/** A rule of a stored quiz: the group, how many to draw, and how many shown questions it holds now. */
export interface QuizRule {
  groupId: number
  group: string
  questionCount: number
  available: number
}

/** Matches QuizDto of the API: everything Quiz Details shows. */
export interface Quiz {
  id: number
  name: string
  subjectId: number
  subject: string
  levelId: number
  typeId: number
  type: string
  durationMin: number
  passRate: number
  status: ActiveStatus
  description: string | null
  questionCount: number
  /** "Topic" or a dimension type of the subject (Domain, Group...). */
  selectionType: string
  rules: QuizRule[]
  /** The set a lesson quiz drew at its last save. */
  fixedQuestionCount: number
  /** Taken: only the status may change (UC-57 A2). */
  isTaken: boolean
}

/** Quiz Details form: the Overview tab, then the Setting tab. */
export interface QuizInput {
  name: string
  status: ActiveStatus
  subjectId: number
  levelId: number
  durationMin: number
  passRate: number
  typeId: number
  description?: string
  questionCount: number
  selectionType: string
  rules: { groupId: number; questionCount: number }[]
}

export interface QuizSubject extends QuestionLookup {
  /** The pass rate a new quiz of the subject starts from (BR-21). */
  passRate: number
}

export interface QuizChoices {
  subjects: QuizSubject[]
  levels: QuestionLookup[]
  types: QuestionLookup[]
}

export interface QuizGroup extends QuestionLookup {
  /** Shown questions the group holds now (UC-57 E2). */
  available: number
}

/** The ways a subject's questions can be drawn, and the groups of the chosen one. */
export interface QuizGroups {
  drawnBy: string[]
  groups: QuizGroup[]
}

export interface QuizCriteria extends ListCriteria {
  subjectId?: number
  typeId?: number
}

/** /api/quizzes — UC-56 View Quizzes List, UC-57 Add or Edit Quiz, UC-58 Delete Quiz. */
export const quizzesApi = {
  list: (criteria: QuizCriteria) => http.get<Page<QuizSummary>>('/quizzes', { params: criteria }).then((r) => r.data),

  filters: () => http.get<QuizFilters>('/quizzes/filters').then((r) => r.data),

  remove: (id: number) => http.delete(`/quizzes/${id}`).then(() => undefined),

  get: (id: number) => http.get<Quiz>(`/quizzes/${id}`).then((r) => r.data),

  choices: () => http.get<QuizChoices>('/quizzes/choices').then((r) => r.data),

  groups: (subjectId: number, drawnBy: string) =>
    http.get<QuizGroups>('/quizzes/groups', { params: { subjectId, drawnBy } }).then((r) => r.data),

  create: (input: QuizInput) => http.post<Quiz>('/quizzes', input).then((r) => r.data),

  update: (id: number, input: QuizInput) => http.put<Quiz>(`/quizzes/${id}`, input).then((r) => r.data),

  /** The one change a taken quiz accepts (UC-57 A2). */
  changeStatus: (id: number, status: ActiveStatus) =>
    http.put<Quiz>(`/quizzes/${id}/status`, { status }).then((r) => r.data),
}
