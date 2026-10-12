import { http } from './client'
import type { RegistrationStatus } from './customerApi'
import type { ListCriteria, Page } from './types'

/** Matches RegistrationSummaryDto (UC-59). */
export interface RegistrationSummary {
  id: number
  email: string
  registeredAt: string
  subjectName: string
  packageName: string
  totalCost: number
  status: RegistrationStatus
  validFrom: string | null
  validTo: string | null
  updatedBy: string
}

export interface RegistrationListCriteria extends ListCriteria {
  subjectId?: number
  /** Vietnam dates, YYYY-MM-DD, both included. */
  from?: string
  to?: string
  status?: RegistrationStatus
}

/** /api/registrations — the Sale screens (F#35, F#36). */
export const saleApi = {
  /** UC-59. 400 To when the range ends before it starts (E1). */
  registrations: (criteria: RegistrationListCriteria) =>
    http.get<Page<RegistrationSummary>>('/registrations', { params: criteria }).then((r) => r.data),

  /** UC-59: the subjects to filter by. */
  registrationFilters: () =>
    http.get<{ subjects: { id: number; name: string }[] }>('/registrations/filters').then((r) => r.data),
}
