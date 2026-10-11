import { useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import {
  accountApi,
  type ChangePasswordInput,
  type CurrentUser,
  type LoginInput,
  type NewPasswordInput,
  type ProfileInput,
  type RegisterInput,
} from '../api/accountApi'

// ['account'] holds who is signed in. Signing in or out refetches it, which also
// renews the antiforgery token for the new user.
const keys = {
  me: ['account', 'me'] as const,
  profile: ['account', 'profile'] as const,
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

/**
 * UC-08. Whatever the answer, the page forgets the user: a session that had already
 * expired (A1) is refused by the antiforgery check, and that must not keep anyone on
 * screen as signed in. Reloading the account then hands out a fresh token.
 */
export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: accountApi.logout,
    onSettled: () => {
      queryClient.removeQueries({ predicate: (q) => q.queryKey[0] !== 'account' })
      queryClient.setQueryData(keys.me, null)
      void queryClient.invalidateQueries({ queryKey: keys.me })
    },
  })
}

export function useRegister() {
  return useMutation({ mutationFn: (input: RegisterInput) => accountApi.register(input) })
}

export function useVerifyEmail() {
  return useMutation({ mutationFn: (token: string) => accountApi.verifyEmail(token) })
}

export function useSendPasswordResetLink() {
  return useMutation({ mutationFn: (email: string) => accountApi.sendPasswordResetLink(email) })
}

/** UC-05 step 2: whether the reset link still works, before the form is shown. */
export function useResetLinkCheck(token: string) {
  return useQuery({
    queryKey: ['account', 'reset-link', token],
    queryFn: () => accountApi.checkResetLink(token),
    enabled: token !== '',
    retry: false,
  })
}

export function useResetPassword(token: string) {
  return useMutation({ mutationFn: (input: NewPasswordInput) => accountApi.resetPassword(token, input) })
}

/** UC-06: loads while the pop-up is open. */
export function useProfile(open: boolean) {
  return useQuery({ queryKey: keys.profile, queryFn: accountApi.profile, enabled: open })
}

/** UC-06. The header shows the name and avatar too, so the account is reloaded with the profile. */
export function useSaveProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: ProfileInput) => accountApi.saveProfile(input),
    onSuccess: (profile) => {
      queryClient.setQueryData(keys.profile, profile)
      void queryClient.invalidateQueries({ queryKey: keys.me })
    },
  })
}

export function useChangePassword() {
  return useMutation({ mutationFn: (input: ChangePasswordInput) => accountApi.changePassword(input) })
}

export function useSendVerificationLink() {
  return useMutation({ mutationFn: (email: string) => accountApi.sendVerificationLink(email) })
}
