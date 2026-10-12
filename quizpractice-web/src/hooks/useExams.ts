import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { examApi } from '../api/examApi'
import type { ListCriteria } from '../api/types'

// Under ['account', ...]: what the customer may take depends on who is signed in.
export const examKeys = {
  all: ['account', 'exams'] as const,
}

/** UC-22. */
export function useExams(criteria: ListCriteria & { subjectId?: number }) {
  return useQuery({
    queryKey: [...examKeys.all, 'list', criteria],
    queryFn: () => examApi.list(criteria),
    placeholderData: keepPreviousData,
  })
}

/** UC-22: the Subject filter; an empty list is E2. */
export function useExamFilters() {
  return useQuery({ queryKey: [...examKeys.all, 'filters'], queryFn: examApi.filters })
}
