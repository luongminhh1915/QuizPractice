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

/** Matches AnswerOptionDto of the API. */
export interface AnswerOption {
  id: number
  content: string
  isCorrect: boolean
}

/** Matches QuestionDto of the API: everything Question Details shows. */
export interface Question {
  id: number
  subjectId: number
  subject: string
  lessonId: number | null
  levelId: number
  status: VisibilityStatus
  dimensionIds: number[]
  content: string
  /** Access-checked address of the media file; empty when the question has none. */
  mediaUrl: string | null
  mediaKind: 'image' | 'audio' | 'video' | null
  explanation: string | null
  options: AnswerOption[]
  /** A quiz or a stored attempt has it: the subject is fixed and a chosen option stays. */
  isUsed: boolean
  /** Sent back on Save: the API refuses it when another user saved in between (UC-53 E4). */
  version: string
}

/** Question Details form. An option keeps its id when it already exists; correctIndex marks the right one. */
export interface QuestionInput {
  subjectId: number
  lessonId?: number
  levelId: number
  status: VisibilityStatus
  dimensionIds?: number[]
  content: string
  explanation?: string
  options: { id?: number; content: string }[]
  correctIndex: number
  /** A new media file; empty keeps the stored one unless removeMedia is set. */
  media?: File
  removeMedia?: boolean
}

function toFormData(input: QuestionInput, version?: string): FormData {
  const data = new FormData()
  data.append('subjectId', String(input.subjectId))
  if (input.lessonId !== undefined && input.lessonId !== null) data.append('lessonId', String(input.lessonId))
  data.append('levelId', String(input.levelId))
  data.append('status', input.status)
  for (const id of input.dimensionIds ?? []) data.append('dimensionIds', String(id))
  data.append('content', input.content)
  data.append('explanation', input.explanation ?? '')
  input.options.forEach((option, i) => {
    if (option.id !== undefined) data.append(`options[${i}].id`, String(option.id))
    data.append(`options[${i}].content`, option.content)
    data.append(`options[${i}].isCorrect`, String(i === input.correctIndex))
  })
  if (version !== undefined) data.append('version', version)
  if (input.media) data.append('media', input.media)
  else if (input.removeMedia) data.append('removeMedia', 'true')
  return data
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

/** /api/questions — UC-52 View Questions List, UC-53 Add or Edit Question, UC-54 Show or Hide Question. */
export const questionsApi = {
  // indexes: null sends dimensionIds=3&dimensionIds=5, the form ASP.NET Core binds to an array.
  list: (criteria: QuestionCriteria) =>
    http
      .get<Page<QuestionSummary>>('/questions', { params: criteria, paramsSerializer: { indexes: null } })
      .then((r) => r.data),

  filters: (subjectId?: number) =>
    http.get<QuestionFilters>('/questions/filters', { params: { subjectId } }).then((r) => r.data),

  get: (id: number) => http.get<Question>(`/questions/${id}`).then((r) => r.data),

  create: (input: QuestionInput) => http.post<Question>('/questions', toFormData(input)).then((r) => r.data),

  update: (id: number, input: QuestionInput, version: string) =>
    http.put<Question>(`/questions/${id}`, toFormData(input, version)).then((r) => r.data),

  changeStatus: (id: number, status: VisibilityStatus) =>
    http.put<QuestionSummary>(`/questions/${id}/status`, { status }).then((r) => r.data),
}
