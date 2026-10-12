import { http } from './client'

export interface Bank {
  bin: string
  shortName: string
  name: string
}

/** Matches BankAccountDto of the API (UC-70); every account field is null before the first save. */
export interface BankAccount {
  bankBin: string | null
  bankName: string | null
  accountNumber: string | null
  accountName: string | null
  updatedAt: string | null
  /** The last change the screen was opened with (E2). */
  version: string | null
  banks: Bank[]
}

export interface BankAccountInput {
  bankBin: string
  accountNumber: string
  accountName: string
}

export interface BankAccountSaved {
  account: BankAccount
  /** False when nothing differed and nothing was stored (A2). */
  changed: boolean
}

export interface SampleQr {
  payload: string
  amount: number
  content: string
}

/** /api/bank-account — UC-70 Edit Receiving Bank Account (Admin only). */
export const bankAccountApi = {
  get: () => http.get<BankAccount>('/bank-account').then((r) => r.data),

  /** 409 BankAccount.Changed: another Admin saved meanwhile (E2). */
  save: (input: BankAccountInput, version: string | null) =>
    http.put<BankAccountSaved>('/bank-account', { ...input, version }).then((r) => r.data),

  sampleQr: (bankBin: string, accountNumber: string) =>
    http.get<SampleQr>('/bank-account/sample-qr', { params: { bankBin, accountNumber } }).then((r) => r.data),
}
