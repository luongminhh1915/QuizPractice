import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { subjectsApi, type NewSubjectInput, type SubjectCriteria } from '../api/subjectsApi'

// Query keys: ['subjects'] covers every list and detail of Course Content, so one
// invalidate after a change reloads whatever subject screen is open.
const keys = {
  all: ['subjects'] as const,
  list: (criteria: SubjectCriteria) => ['subjects', 'list', criteria] as const,
  filters: ['subjects', 'filters'] as const,
  choices: ['subjects', 'choices'] as const,
  detail: (id: number) => ['subjects', 'detail', id] as const,
}

export function useSubjectList(criteria: SubjectCriteria) {
  return useQuery({
    queryKey: keys.list(criteria),
    queryFn: () => subjectsApi.list(criteria),
    // Keep the current rows on screen while the next page or sort loads.
    placeholderData: keepPreviousData,
  })
}

/** The subject categories of the filter (BR-16). */
export function useSubjectFilters() {
  return useQuery({ queryKey: keys.filters, queryFn: subjectsApi.filters, staleTime: 5 * 60_000 })
}

/** Categories and, for the Admin, the Experts; always fresh so a lost Expert role shows (UC-42 E2). */
export function useSubjectChoices() {
  return useQuery({ queryKey: keys.choices, queryFn: subjectsApi.choices })
}

export function useSubject(id: number | undefined) {
  return useQuery({ queryKey: keys.detail(id ?? 0), queryFn: () => subjectsApi.get(id!), enabled: id !== undefined })
}

/** UC-42: the Admin creates a subject. */
export function useCreateSubject() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: NewSubjectInput) => subjectsApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}
