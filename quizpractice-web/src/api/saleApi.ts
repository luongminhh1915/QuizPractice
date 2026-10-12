import { http } from './client'
import type { InvoiceStatus, RegistrationStatus } from './customerApi'
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

/** Matches SalePackageDto. */
export interface SalePackage {
  id: number
  name: string
  accessMonths: number
  listPrice: number
  salePrice: number
}

export type Gender = 'Male' | 'Female' | 'Other'

/** Matches RegistrationDetailsDto (UC-60, UC-61). */
export interface RegistrationDetails {
  id: number
  subjectId: number
  subjectName: string
  package: SalePackage
  fullName: string
  gender: Gender
  email: string
  mobile: string
  totalCost: number
  status: RegistrationStatus
  registeredAt: string
  /** Empty for an online registration. */
  recordedBy: string | null
  updatedBy: string
  validFrom: string | null
  validTo: string | null
  notes: string | null
  invoice: { id: number; code: string; status: InvoiceStatus; totalAmount: number } | null
  hasAccount: boolean
  /** This Sale user recorded it and it is still Submitted, in no unpaid invoice (BR-22). */
  canEditAll: boolean
  /** The status may still change here. */
  canSettle: boolean
}

/** Matches SaveRegistrationRequest. */
export interface RegistrationInput {
  subjectId: number
  packageId: number
  fullName: string
  gender: Gender
  email: string
  mobile: string
  notes?: string
}

/** Matches RegistrationStatusResultDto (UC-61). mailFailed: E2, the customer uses Forgot Password. */
export interface RegistrationStatusResult {
  registration: RegistrationDetails
  accountCreated: boolean
  mailFailed: boolean
}

/** Matches SaleInvoiceSummaryDto (UC-68). */
export interface SaleInvoiceSummary {
  id: number
  code: string
  customerEmail: string
  createdAt: string
  registrationCount: number
  totalAmount: number
  status: InvoiceStatus
  paidAt: string | null
  confirmedBy: string | null
}

export interface InvoiceListCriteria extends ListCriteria {
  status?: InvoiceStatus
  from?: string
  to?: string
}

/** /api/registrations and /api/invoices — the Sale screens (F#35, F#36; UC-68, UC-69). */
export const saleApi = {
  /** UC-59. 400 To when the range ends before it starts (E1). */
  registrations: (criteria: RegistrationListCriteria) =>
    http.get<Page<RegistrationSummary>>('/registrations', { params: criteria }).then((r) => r.data),

  /** UC-60: the subjects on sale and their active packages. */
  registrationChoices: () =>
    http
      .get<{ subjects: { id: number; name: string; packages: SalePackage[] }[] }>('/registrations/choices')
      .then((r) => r.data),

  registration: (id: number) => http.get<RegistrationDetails>(`/registrations/${id}`).then((r) => r.data),

  /** UC-60. 400 Email for a staff account; 409 Package.NotAvailable (E2). */
  createRegistration: (input: RegistrationInput) => http.post<RegistrationDetails>('/registrations', input).then((r) => r.data),

  /** UC-60: the whole form. 409 Registration.NotEditable (BR-22) or Registration.Changed (E3). */
  updateRegistration: (id: number, input: RegistrationInput) =>
    http.put<RegistrationDetails>(`/registrations/${id}`, input).then((r) => r.data),

  /** UC-61: Paid or Cancelled. 409 InUnpaidInvoice (A3), Settled (A4), NoDuration (E1), Changed (E3). */
  changeRegistrationStatus: (id: number, status: RegistrationStatus, notes: string | undefined) =>
    http.put<RegistrationStatusResult>(`/registrations/${id}/status`, { status, notes }).then((r) => r.data),

  /** BR-22: the notes alone, at any status. */
  saveRegistrationNotes: (id: number, notes: string | undefined) =>
    http.put<RegistrationDetails>(`/registrations/${id}/notes`, { notes }).then((r) => r.data),

  /** UC-68: the unpaid ones first, then the newest. */
  invoices: (criteria: InvoiceListCriteria) => http.get<Page<SaleInvoiceSummary>>('/invoices', { params: criteria }).then((r) => r.data),

  /** UC-59: the subjects to filter by. */
  registrationFilters: () =>
    http.get<{ subjects: { id: number; name: string }[] }>('/registrations/filters').then((r) => r.data),
}
