import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usersApi, type NewUserInput, type UserCriteria, type UserEditInput } from '../api/usersApi'

// Query keys: ['users'] covers every list and detail, so one invalidate after a change
// reloads whatever user screen is open.
const keys = {
  all: ['users'] as const,
  list: (criteria: UserCriteria) => ['users', 'list', criteria] as const,
  choices: ['users', 'choices'] as const,
  detail: (id: number) => ['users', 'detail', id] as const,
}

export function useUserList(criteria: UserCriteria) {
  return useQuery({
    queryKey: keys.list(criteria),
    queryFn: () => usersApi.list(criteria),
    placeholderData: keepPreviousData,
  })
}

/** The user roles (BR-16). */
export function useUserChoices() {
  return useQuery({ queryKey: keys.choices, queryFn: usersApi.choices, staleTime: 5 * 60_000 })
}

export function useUser(id: number | undefined) {
  return useQuery({ queryKey: keys.detail(id ?? 0), queryFn: () => usersApi.get(id!), enabled: id !== undefined })
}

// A new owner list follows a role change (UC-63 A3), so the subject screens reload too.
export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: NewUserInput) => usersApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === 'users' || q.queryKey[0] === 'subjects' }),
  })
}

export function useUpdateUser(id: number | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: UserEditInput) => usersApi.update(id!, input),
    onSuccess: () => queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] === 'users' || q.queryKey[0] === 'subjects' }),
  })
}
