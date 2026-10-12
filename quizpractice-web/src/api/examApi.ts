import { http } from './client'
import type { ListCriteria, Page } from './types'

/** Matches ExamSummaryDto (UC-22). */
export interface ExamSummary {
  id: number
  subjectName: string
  name: string
  level: string
  questionCount: number
  durationMin: number
  passRate: number
}

/** Matches ExamDetailsDto (UC-23). openAttemptId: the unfinished attempt to resume (A2). */
export interface ExamDetails extends ExamSummary {
  description: string | null
  openAttemptId: number | null
}

/** /api/simulation-exams — Simulation Exams (F#16). */
export const examApi = {
  /** UC-22: active simulation exams of the subjects the customer may reach. */
  list: (criteria: ListCriteria & { subjectId?: number }) =>
    http.get<Page<ExamSummary>>('/simulation-exams', { params: criteria }).then((r) => r.data),

  /** UC-23 step 2. */
  get: (id: number) => http.get<ExamDetails>(`/simulation-exams/${id}`).then((r) => r.data),

  /** UC-23: a new attempt. 403 Subject.NoAccess (E1), 409 Exam.Unfinished (A2) or Exam.NotAvailable (E2). */
  start: (id: number) => http.post<{ id: number }>(`/simulation-exams/${id}/start`).then((r) => r.data),

  /** UC-22: the subjects the customer may reach; empty is E2. */
  filters: () => http.get<{ subjects: { id: number; name: string }[] }>('/simulation-exams/filters').then((r) => r.data),
}
