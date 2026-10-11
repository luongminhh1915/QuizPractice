import { useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { accountApi, type CurrentUser, type LoginInput } from '../api/accountApi'

// ['account'] holds who is signed in. Signing in or out refetches it, which also
// renews the antiforgery token for the new user.
const keys = {
  me: ['account', 'me'] as const,
}

/** The signed-in user; data is null for a Guest. Both layouts call it, so it loads on every page. */
export function useCurrentUser() {
  return useQuery({ queryKey: keys.me, queryFn: accountApi.me, staleTime: 5 * 60 * 1000 })
}

/**
 * Another user now: keep the new account, and reload whatever the screens show,
 * because the lists and details of the previous user must not stay on screen.
 */
function switchUser(queryClient: QueryClient, user: CurrentUser | null) {
  queryClient.setQueryData(keys.me, user)
  // Not awaited: the pop-up closes at once and the screens reload behind it.
  void queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] !== 'account' })
}

export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: LoginInput) => accountApi.login(input),
    onSuccess: (user) => switchUser(queryClient, user),
  })
}

export function useSendVerificationLink() {
  return useMutation({ mutationFn: (email: string) => accountApi.sendVerificationLink(email) })
}
