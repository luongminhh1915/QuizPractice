import { useQuery } from '@tanstack/react-query'
import { accountApi } from '../api/accountApi'

// ['account'] holds who is signed in. Signing in or out refetches it, which also
// renews the antiforgery token for the new user.
const keys = {
  me: ['account', 'me'] as const,
}

/** The signed-in user; data is null for a Guest. Both layouts call it, so it loads on every page. */
export function useCurrentUser() {
  return useQuery({ queryKey: keys.me, queryFn: accountApi.me, staleTime: 5 * 60 * 1000 })
}
