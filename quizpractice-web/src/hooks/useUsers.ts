import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { usersApi, type UserCriteria } from '../api/usersApi'

// Query keys: ['users'] covers every list and detail, so one invalidate after a change
// reloads whatever user screen is open.
const keys = {
  all: ['users'] as const,
  list: (criteria: UserCriteria) => ['users', 'list', criteria] as const,
  choices: ['users', 'choices'] as const,
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
