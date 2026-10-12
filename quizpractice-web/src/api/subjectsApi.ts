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

/** Matches SubjectDto of the API: the Overview tab of Subject Management Details. */
export interface Subject {
  id: number
  name: string
  tagline: string | null
  brief: string
  thumbnail: string
  categoryId: number
  category: string
  isFeatured: boolean
  passRate: number
  status: SubjectStatus
  ownerId: number | null
  owner: string | null
  /** HTML cleaned by the API. */
  description: string
  /** Without one the public pages show no price (UC-45 E1). */
  hasActivePackage: boolean
}

/** Drop-downs: subject categories, and the Experts (filled for the Admin only). */
export interface SubjectChoices {
  categories: Lookup[]
  experts: Lookup[]
}

/** New Subject (UC-42). */
export interface NewSubjectInput {
  thumbnail?: File
  name: string
  categoryId: number
  expertId?: number
  isFeatured: boolean
  passRate: number
  description?: string
}

/** Overview tab (UC-44): leaving thumbnail empty keeps the stored image. */
export interface SubjectOverviewInput {
  thumbnail?: File
  name: string
  tagline?: string
  brief?: string
  categoryId: number
  isFeatured: boolean
  passRate: number
  description?: string
  /** The owner as the form shows it; only the Admin may change it (UC-43). */
  expertId?: number | null
  /** Only the Admin may change it (UC-45). */
  status: SubjectStatus
  /** The Admin confirmed publishing without an active package (UC-45 E1). */
  confirmNoPackage?: boolean
}

function overviewForm(input: SubjectOverviewInput): FormData {
  const data = new FormData()
  data.append('name', input.name)
  data.append('tagline', input.tagline ?? '')
  data.append('brief', input.brief ?? '')
  data.append('categoryId', String(input.categoryId))
  data.append('isFeatured', String(input.isFeatured))
  data.append('passRate', String(input.passRate))
  data.append('description', input.description ?? '')
  // Absent: the subject has no owner (UC-42 A2), or the Admin removed it.
  if (input.expertId) data.append('expertId', String(input.expertId))
  data.append('status', input.status)
  if (input.confirmNoPackage) data.append('confirmNoPackage', 'true')
  if (input.thumbnail) data.append('thumbnail', input.thumbnail)
  return data
}

function newSubjectForm(input: NewSubjectInput): FormData {
  const data = new FormData()
  data.append('name', input.name)
  data.append('categoryId', String(input.categoryId))
  if (input.expertId) data.append('expertId', String(input.expertId))
  data.append('isFeatured', String(input.isFeatured))
  data.append('passRate', String(input.passRate))
  data.append('description', input.description ?? '')
  if (input.thumbnail) data.append('thumbnail', input.thumbnail)
  return data
}

/** A dimension of a subject (UC-46). */
export interface Dimension {
  id: number
  typeId: number
  /** Dimension Type setting, such as Domain or Group. */
  type: string
  name: string
  description: string | null
}

/** The Dimension tab: the types the form offers and the stored dimensions. */
export interface DimensionTab {
  types: Lookup[]
  dimensions: Dimension[]
}

export interface DimensionInput {
  typeId: number
  name: string
  description?: string
}

/** /api/subjects — UC-41 to UC-46 (Course Content). */
export const subjectsApi = {
  list: (criteria: SubjectCriteria) => http.get<Page<SubjectSummary>>('/subjects', { params: criteria }).then((r) => r.data),

  filters: () => http.get<SubjectFilters>('/subjects/filters').then((r) => r.data),

  choices: () => http.get<SubjectChoices>('/subjects/choices').then((r) => r.data),

  get: (id: number) => http.get<Subject>(`/subjects/${id}`).then((r) => r.data),

  create: (input: NewSubjectInput) => http.post<Subject>('/subjects', newSubjectForm(input)).then((r) => r.data),

  update: (id: number, input: SubjectOverviewInput) =>
    http.put<Subject>(`/subjects/${id}`, overviewForm(input)).then((r) => r.data),

  dimensions: (subjectId: number) => http.get<DimensionTab>(`/subjects/${subjectId}/dimensions`).then((r) => r.data),

  saveDimension: (subjectId: number, id: number | undefined, input: DimensionInput) =>
    (id === undefined
      ? http.post<Dimension>(`/subjects/${subjectId}/dimensions`, input)
      : http.put<Dimension>(`/subjects/${subjectId}/dimensions/${id}`, input)
    ).then((r) => r.data),

  /** 409 Dimension.InUse names how many questions, practices and quiz rules keep it (UC-46 E3). */
  deleteDimension: (subjectId: number, id: number) =>
    http.delete(`/subjects/${subjectId}/dimensions/${id}`).then(() => undefined),
}
