import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { subjectsApi, type SubjectCriteria } from '../api/subjectsApi'

// Query keys: ['subjects'] covers every list and detail of Course Content, so one
// invalidate after a change reloads whatever subject screen is open.
const keys = {
  all: ['subjects'] as const,
  list: (criteria: SubjectCriteria) => ['subjects', 'list', criteria] as const,
  filters: ['subjects', 'filters'] as const,
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
