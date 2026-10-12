import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
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

/** UC-23 step 2: loads while the details are open. */
export function useExam(id: number | undefined) {
  return useQuery({
    queryKey: [...examKeys.all, 'detail', id ?? 0],
    queryFn: () => examApi.get(id!),
    enabled: id !== undefined,
    retry: false,
    staleTime: 0,
  })
}

/** UC-23 steps 3 to 5. Whatever the answer, the exams and the practices reload. */
export function useStartExam() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => examApi.start(id),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: examKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['account', 'practices'] })
    },
  })
}

/** UC-22: the Subject filter; an empty list is E2. */
export function useExamFilters() {
  return useQuery({ queryKey: [...examKeys.all, 'filters'], queryFn: examApi.filters })
}
