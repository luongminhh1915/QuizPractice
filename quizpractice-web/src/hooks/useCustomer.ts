import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { customerApi } from '../api/customerApi'

// The customer's own records live under ['account', ...], so signing in as someone else
// (switchUser in useAccount) drops them with the rest of the account.
export const customerKeys = {
  registrations: ['account', 'my-registrations'] as const,
  changeForm: (id: number) => ['account', 'change-form', id] as const,
  invoices: ['account', 'my-invoices'] as const,
  invoice: (id: number) => ['account', 'my-invoices', 'detail', id] as const,
}

/** UC-15. */
export function useMyRegistrations(page: number, pageSize: number) {
  return useQuery({
    queryKey: [...customerKeys.registrations, page, pageSize],
    queryFn: () => customerApi.registrations(page, pageSize),
    placeholderData: keepPreviousData,
  })
}

/** UC-18: makes the invoice; the registrations then show it. */
export function useCreateInvoice() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (registrationIds: number[]) => customerApi.createInvoice(registrationIds),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: customerKeys.registrations })
      void queryClient.invalidateQueries({ queryKey: customerKeys.invoices })
    },
  })
}

/** Invoice QR (2.7). Not retried: a 404 shows at once. */
export function useMyInvoice(id: number) {
  return useQuery({ queryKey: customerKeys.invoice(id), queryFn: () => customerApi.invoice(id), retry: false })
}

/** UC-18 A2: the invoice, its registrations and the list all change. */
export function useCancelInvoice(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => customerApi.cancelInvoice(id),
    onSuccess: (invoice) => queryClient.setQueryData(customerKeys.invoice(id), invoice),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: customerKeys.registrations })
      void queryClient.invalidateQueries({ queryKey: customerKeys.invoices, predicate: (q) => q.queryKey[2] !== 'detail' })
    },
  })
}

/** UC-17. Whatever the answer, the list reloads to the current state (E1, E2). */
export function useCancelRegistration() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => customerApi.cancel(id),
    onSettled: () => queryClient.invalidateQueries({ queryKey: customerKeys.registrations }),
  })
}

/** UC-16: loads while the pop-up is open. */
export function useChangeForm(id: number, open: boolean) {
  return useQuery({ queryKey: customerKeys.changeForm(id), queryFn: () => customerApi.changeForm(id), enabled: open })
}

export function useChangeRegistration(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (packageId: number) => customerApi.change(id, packageId),
    // Settled or not, the list shows the current state afterwards (UC-16 E1, E3).
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: customerKeys.registrations })
      void queryClient.invalidateQueries({ queryKey: customerKeys.changeForm(id) })
    },
  })
}
