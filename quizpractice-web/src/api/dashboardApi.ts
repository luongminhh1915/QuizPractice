import { http } from './client'

/** One day of the order trend: orders made that day, and how many of them are Paid now. */
export interface TrendPoint {
  date: string
  paid: number
  all: number
}

/** Where a card leads for the signed-in role; empty when it is not a link (SRS III 5.1, OI-10). */
export interface DashboardLinks {
  subjects: string | null
  registrations: string | null
  revenue: string | null
  customers: string | null
}

/** Matches DashboardDto of the API (UC-34). Dates are Vietnam days, yyyy-MM-dd. */
export interface Dashboard {
  from: string
  to: string
  newSubjects: number
  totalSubjects: number
  paidRegistrations: number
  submittedRegistrations: number
  cancelledRegistrations: number
  revenue: number
  newCustomers: number
  newBuyers: number
  trend: TrendPoint[]
  revenueByCategory: { category: string; revenue: number }[]
  links: DashboardLinks
}

/** Both ends of the range, or neither for the last seven days. */
export interface DashboardRange {
  from?: string
  to?: string
}

/** /api/dashboard — UC-34 View Management Dashboard. */
export const dashboardApi = {
  get: (range: DashboardRange) => http.get<Dashboard>('/dashboard', { params: range }).then((r) => r.data),
}
