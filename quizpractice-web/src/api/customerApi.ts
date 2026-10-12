import { http } from './client'
import type { RegisterForm } from './publicApi'
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

export type InvoiceStatus = 'Unpaid' | 'Paid' | 'Cancelled'

/** Matches InvoiceDto (UC-18, UC-67). qrPayload is set only while Unpaid. */
export interface Invoice {
  id: number
  code: string
  status: InvoiceStatus
  createdAt: string
  paidAt: string | null
  totalAmount: number
  bankName: string
  bankBin: string
  accountNumber: string
  accountName: string
  qrPayload: string | null
  registrations: { id: number; subjectName: string; packageName: string; totalCost: number }[]
}

/** Matches InvoiceSummaryDto (UC-67). */
export interface InvoiceSummary {
  id: number
  code: string
  createdAt: string
  registrationCount: number
  totalAmount: number
  status: InvoiceStatus
  paidAt: string | null
}

/** /api/my — the signed-in customer's own records (Customer feature). */
export const customerApi = {
  /** UC-15. */
  registrations: (page: number, pageSize: number) =>
    http.get<Page<MyRegistration>>('/my/registrations', { params: { page, pageSize } }).then((r) => r.data),

  /** UC-16: the Subject Register pop-up on the stored package. */
  changeForm: (id: number) => http.get<RegisterForm>(`/my/registrations/${id}/form`).then((r) => r.data),

  /** UC-67. */
  invoices: (status: InvoiceStatus | undefined, page: number, pageSize: number) =>
    http.get<Page<InvoiceSummary>>('/my/invoices', { params: { status, page, pageSize } }).then((r) => r.data),

  /** UC-18: one invoice for the chosen registrations. 409 when one is no longer payable (E1) or no account is set (E2). */
  createInvoice: (registrationIds: number[]) => http.post<Invoice>('/my/invoices', { registrationIds }).then((r) => r.data),

  /** Invoice QR (2.7). */
  invoice: (id: number) => http.get<Invoice>(`/my/invoices/${id}`).then((r) => r.data),

  /** UC-18 A2. */
  cancelInvoice: (id: number) => http.post<Invoice>(`/my/invoices/${id}/cancel`).then((r) => r.data),

  /** UC-17. 409 Registration.Settled (E1) or Registration.InUnpaidInvoice (E2). */
  cancel: (id: number) => http.post<MyRegistration>(`/my/registrations/${id}/cancel`).then((r) => r.data),

  /** UC-16. 409 Registration.Settled (E1) or Registration.InUnpaidInvoice (E3). */
  change: (id: number, packageId: number) =>
    http.put<MyRegistration>(`/my/registrations/${id}`, { packageId }).then((r) => r.data),
}
