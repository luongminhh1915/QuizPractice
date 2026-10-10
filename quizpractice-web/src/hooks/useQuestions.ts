import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { questionsApi, type QuestionCriteria } from '../api/questionsApi'

// Query keys: ['questions'] covers every list and detail, so one invalidate after a
// change reloads whatever question screen is open.
const keys = {
  all: ['questions'] as const,
  list: (criteria: QuestionCriteria) => ['questions', 'list', criteria] as const,
  filters: (subjectId?: number) => ['questions', 'filters', subjectId ?? null] as const,
}

export function useQuestionList(criteria: QuestionCriteria) {
  return useQuery({
    queryKey: keys.list(criteria),
    queryFn: () => questionsApi.list(criteria),
    // Keep the current rows on screen while the next page or sort loads.
    placeholderData: keepPreviousData,
  })
}

/** Subjects and levels, plus the lessons and dimensions of subjectId when one is chosen. */
export function useQuestionFilters(subjectId?: number) {
  return useQuery({
    queryKey: keys.filters(subjectId),
    queryFn: () => questionsApi.filters(subjectId),
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  })
}
