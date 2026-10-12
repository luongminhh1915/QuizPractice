import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { practiceApi, type CreatePractice, type PracticeListCriteria, type SelectionType } from '../api/practiceApi'

// Under ['account', ...] like the other records of the customer, so signing in as
// someone else drops them (switchUser in useAccount).
export const practiceKeys = {
  all: ['account', 'practices'] as const,
  form: ['account', 'practices', 'form'] as const,
  detail: (id: number) => ['account', 'practices', 'detail', id] as const,
  groups: (subjectId: number, selectedBy: SelectionType) => ['account', 'practices', 'groups', subjectId, selectedBy] as const,
}

/** UC-19. */
export function usePractices(criteria: PracticeListCriteria) {
  return useQuery({
    queryKey: [...practiceKeys.all, 'list', criteria],
    queryFn: () => practiceApi.list(criteria),
    placeholderData: keepPreviousData,
  })
}

/** UC-19: the Subject filter. */
export function usePracticeFilters() {
  return useQuery({ queryKey: [...practiceKeys.all, 'filters'], queryFn: practiceApi.filters })
}

/** UC-21. */
export function usePractice(id: number) {
  return useQuery({ queryKey: practiceKeys.detail(id), queryFn: () => practiceApi.get(id), retry: false })
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
