import { http } from './client'
import type { Page } from './types'

export type RegistrationStatus = 'Submitted' | 'Paid' | 'Cancelled'

/** Matches MyRegistrationDto (UC-15). */
export interface MyRegistration {
  id: number
  subjectId: number
  subjectName: string
  packageId: number
  packageName: string
  totalCost: number
  registeredAt: string
  status: RegistrationStatus
  validFrom: string | null
  validTo: string | null
  /** The unpaid invoice it waits in; Edit and Cancel are not offered meanwhile (BR-24). */
  unpaidInvoice: { id: number; code: string } | null
  /** Submitted and in no unpaid invoice: Edit, Cancel and Pay by QR are offered. */
  canChange: boolean
}

/** /api/my — the signed-in customer's own records (Customer feature). */
export const customerApi = {
  /** UC-15. */
  registrations: (page: number, pageSize: number) =>
    http.get<Page<MyRegistration>>('/my/registrations', { params: { page, pageSize } }).then((r) => r.data),
}
