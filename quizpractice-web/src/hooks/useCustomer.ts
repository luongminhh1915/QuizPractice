import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { customerApi } from '../api/customerApi'

// The customer's own records live under ['account', ...], so signing in as someone else
// (switchUser in useAccount) drops them with the rest of the account.
export const customerKeys = {
  registrations: ['account', 'my-registrations'] as const,
  changeForm: (id: number) => ['account', 'change-form', id] as const,
}

/** UC-15. */
export function useMyRegistrations(page: number, pageSize: number) {
  return useQuery({
    queryKey: [...customerKeys.registrations, page, pageSize],
    queryFn: () => customerApi.registrations(page, pageSize),
    placeholderData: keepPreviousData,
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
