import { http } from './client'
import type { ListCriteria, Page } from './types'

/** Subject (BR-12, BR-23): only a Published subject reaches the public catalog. */
export type SubjectStatus = 'Published' | 'Unpublished'

/** A setting or a user offered in a drop-down. */
export interface Lookup {
  id: number
  name: string
}

/** Matches SubjectSummaryDto of the API: a row of Subject Management List. */
export interface SubjectSummary {
  id: number
  name: string
  categoryId: number
  category: string
  /** Lessons and quizzes; a Subject Topic only groups them. */
  lessonCount: number
  ownerId: number | null
  /** null: nobody prepares the subject yet (UC-42 A2). */
  owner: string | null
  status: SubjectStatus
}

export interface SubjectFilters {
  categories: Lookup[]
}

export interface SubjectCriteria extends ListCriteria {
  categoryId?: number
  status?: SubjectStatus
}

/** /api/subjects — UC-41 to UC-45 (Course Content). */
export const subjectsApi = {
  list: (criteria: SubjectCriteria) => http.get<Page<SubjectSummary>>('/subjects', { params: criteria }).then((r) => r.data),

  filters: () => http.get<SubjectFilters>('/subjects/filters').then((r) => r.data),
}
