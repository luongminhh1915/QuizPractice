import { http } from './client'
import type { ListCriteria, Page, VisibilityStatus } from './types'

/** Matches QuestionSummaryDto of the API: a row of Questions List. */
export interface QuestionSummary {
  id: number
  content: string
  subjectId: number
  subject: string
  lessonId: number | null
  lesson: string | null
  dimensions: string[]
  levelId: number
  level: string
  status: VisibilityStatus
}

export interface QuestionLookup {
  id: number
  name: string
}

export interface QuestionLesson extends QuestionLookup {
  topic: string
}

export interface QuestionDimension extends QuestionLookup {
  /** Dimension Type setting: Domain, Group… */
  type: string
}

/** Choices of the filters: subjects the user may reach (BR-11); lessons and dimensions of the chosen subject. */
export interface QuestionFilters {
  subjects: QuestionLookup[]
  levels: QuestionLookup[]
  lessons: QuestionLesson[]
  dimensions: QuestionDimension[]
}

export interface QuestionCriteria extends ListCriteria {
  subjectId?: number
  lessonId?: number
  /** A question matches when it tests at least one of them. */
  dimensionIds?: number[]
  levelId?: number
  status?: VisibilityStatus
}

/** /api/questions — UC-52 View Questions List, UC-54 Show or Hide Question. */
export const questionsApi = {
  // indexes: null sends dimensionIds=3&dimensionIds=5, the form ASP.NET Core binds to an array.
  list: (criteria: QuestionCriteria) =>
    http
      .get<Page<QuestionSummary>>('/questions', { params: criteria, paramsSerializer: { indexes: null } })
      .then((r) => r.data),

  filters: (subjectId?: number) =>
    http.get<QuestionFilters>('/questions/filters', { params: { subjectId } }).then((r) => r.data),

  changeStatus: (id: number, status: VisibilityStatus) =>
    http.put<QuestionSummary>(`/questions/${id}/status`, { status }).then((r) => r.data),
}
