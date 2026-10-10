import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { quizzesApi, type QuizCriteria } from '../api/quizzesApi'

// Query keys: ['quizzes'] covers every list and detail, so one invalidate after a
// change reloads whatever quiz screen is open.
const keys = {
  all: ['quizzes'] as const,
  list: (criteria: QuizCriteria) => ['quizzes', 'list', criteria] as const,
  filters: ['quizzes', 'filters'] as const,
}

export function useQuizList(criteria: QuizCriteria) {
  return useQuery({
    queryKey: keys.list(criteria),
    queryFn: () => quizzesApi.list(criteria),
    // Keep the current rows on screen while the next page or sort loads.
    placeholderData: keepPreviousData,
  })
}

/** The subjects the user may reach and the quiz types. */
export function useQuizFilters() {
  return useQuery({ queryKey: keys.filters, queryFn: quizzesApi.filters, staleTime: 5 * 60_000 })
}

/** UC-58: the API refuses a taken quiz (E2) or one a lesson opens (E3). */
export function useDeleteQuiz() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => quizzesApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}
