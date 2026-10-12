import { http } from './client'

export type AttemptMode = 'Practice' | 'Exam'

/** Matches HandleOptionDto: never says which option is correct. */
export interface HandleOption {
  id: number
  letter: string
  content: string
}

/** Matches HandleQuestionDto. */
export interface HandleQuestion {
  order: number
  questionId: number
  content: string
  mediaKind: 'image' | 'audio' | 'video' | null
  options: HandleOption[]
  chosenOptionId: number | null
  isMarked: boolean
}

/** Matches QuizHandleDto (SRS III 4.1). endsAt only for a simulation exam (BR-06). */
export interface QuizHandle {
  id: number
  subjectName: string
  title: string
  mode: AttemptMode
  startedAt: string
  endsAt: string | null
  serverNow: string
  currentOrder: number
  questions: HandleQuestion[]
}

/** Matches HandleProgressDto. */
export interface HandleProgress {
  order: number
  chosenOptionId: number | null
  isMarked: boolean
}

/** The address of the media of a question in an attempt. */
export const mediaUrl = (id: number, order: number) => `/api/attempts/${id}/questions/${order}/media`

/** /api/attempts — Quiz Handle (F#17). Every change is stored at once (BR-08). */
export const attemptApi = {
  /** UC-24. 409 Attempt.Closed / Attempt.TimeUp, 403 Attempt.AccessEnded. */
  get: (id: number) => http.get<QuizHandle>(`/attempts/${id}`).then((r) => r.data),

  /** UC-24 step 3, A1. */
  answer: (id: number, order: number, optionId: number) =>
    http.put<HandleProgress>(`/attempts/${id}/questions/${order}/answer`, { optionId }).then((r) => r.data),
}
