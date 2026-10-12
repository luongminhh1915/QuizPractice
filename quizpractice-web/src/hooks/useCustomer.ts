import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { customerApi } from '../api/customerApi'

// The customer's own records live under ['account', ...], so signing in as someone else
// (switchUser in useAccount) drops them with the rest of the account.
export const customerKeys = {
  registrations: ['account', 'my-registrations'] as const,
}

/** UC-15. */
export function useMyRegistrations(page: number, pageSize: number) {
  return useQuery({
    queryKey: [...customerKeys.registrations, page, pageSize],
    queryFn: () => customerApi.registrations(page, pageSize),
    placeholderData: keepPreviousData,
  })
}
