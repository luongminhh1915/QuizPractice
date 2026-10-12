import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { practiceApi, type CreatePractice, type SelectionType } from '../api/practiceApi'

// Under ['account', ...] like the other records of the customer, so signing in as
// someone else drops them (switchUser in useAccount).
export const practiceKeys = {
  all: ['account', 'practices'] as const,
  form: ['account', 'practices', 'form'] as const,
  groups: (subjectId: number, selectedBy: SelectionType) => ['account', 'practices', 'groups', subjectId, selectedBy] as const,
}

/** UC-20 step 2. */
export function usePracticeForm() {
  return useQuery({ queryKey: practiceKeys.form, queryFn: practiceApi.form })
}

/** UC-20 step 3: loads once a subject is chosen. */
export function usePracticeGroups(subjectId: number | undefined, selectedBy: SelectionType) {
  return useQuery({
    queryKey: practiceKeys.groups(subjectId ?? 0, selectedBy),
    queryFn: () => practiceApi.groups(subjectId!, selectedBy),
    enabled: subjectId !== undefined,
  })
}

/** UC-20 steps 4 and 5. */
export function useCreatePractice() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (practice: CreatePractice) => practiceApi.create(practice),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: practiceKeys.all }),
  })
}
