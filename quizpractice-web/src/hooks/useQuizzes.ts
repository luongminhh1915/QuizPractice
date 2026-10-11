import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { quizzesApi, type QuizCriteria, type QuizInput } from '../api/quizzesApi'
import type { ActiveStatus } from '../api/types'

// Query keys: ['quizzes'] covers every list and detail, so one invalidate after a
// change reloads whatever quiz screen is open.
const keys = {
  all: ['quizzes'] as const,
  list: (criteria: QuizCriteria) => ['quizzes', 'list', criteria] as const,
  filters: ['quizzes', 'filters'] as const,
  detail: (id: number) => ['quizzes', 'detail', id] as const,
  choices: ['quizzes', 'choices'] as const,
  groups: (subjectId: number, drawnBy: string) => ['quizzes', 'groups', subjectId, drawnBy] as const,
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

export function useQuiz(id: number | undefined) {
  return useQuery({ queryKey: keys.detail(id ?? 0), queryFn: () => quizzesApi.get(id!), enabled: id !== undefined })
}

/** Subjects with their pass rate, levels and quiz types (Overview tab). */
export function useQuizChoices() {
  return useQuery({ queryKey: keys.choices, queryFn: quizzesApi.choices, staleTime: 5 * 60_000 })
}

/** The ways of drawing and the groups with their shown questions (Setting tab); always fresh (E2). */
export function useQuizGroups(subjectId: number | undefined, drawnBy: string | undefined) {
  return useQuery({
    queryKey: keys.groups(subjectId ?? 0, drawnBy ?? ''),
    queryFn: () => quizzesApi.groups(subjectId!, drawnBy!),
    enabled: subjectId !== undefined && !!drawnBy,
    placeholderData: keepPreviousData,
  })
}

export function useSaveQuiz(id: number | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: QuizInput) => (id === undefined ? quizzesApi.create(input) : quizzesApi.update(id, input)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}

/** UC-57 A2: the status of a taken quiz. */
export function useChangeQuizStatus(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (status: ActiveStatus) => quizzesApi.changeStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}
