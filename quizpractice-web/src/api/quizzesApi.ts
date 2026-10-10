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

export interface QuizCriteria extends ListCriteria {
  subjectId?: number
  typeId?: number
}

/** /api/quizzes — UC-56 View Quizzes List, UC-58 Delete Quiz. */
export const quizzesApi = {
  list: (criteria: QuizCriteria) => http.get<Page<QuizSummary>>('/quizzes', { params: criteria }).then((r) => r.data),

  filters: () => http.get<QuizFilters>('/quizzes/filters').then((r) => r.data),

  remove: (id: number) => http.delete(`/quizzes/${id}`).then(() => undefined),
}
