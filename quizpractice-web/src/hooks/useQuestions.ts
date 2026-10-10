import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { questionsApi, type QuestionCriteria, type QuestionInput } from '../api/questionsApi'
import type { VisibilityStatus } from '../api/types'

// Query keys: ['questions'] covers every list and detail, so one invalidate after a
// change reloads whatever question screen is open.
const keys = {
  all: ['questions'] as const,
  list: (criteria: QuestionCriteria) => ['questions', 'list', criteria] as const,
  filters: (subjectId?: number) => ['questions', 'filters', subjectId ?? null] as const,
  detail: (id: number) => ['questions', 'detail', id] as const,
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

export function useQuestion(id: number | undefined) {
  return useQuery({
    queryKey: keys.detail(id ?? 0),
    queryFn: () => questionsApi.get(id!),
    enabled: id !== undefined,
  })
}

/** version: the one the question was loaded with; required on edit (UC-53 E4). */
export function useSaveQuestion(id: number | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ input, version }: { input: QuestionInput; version?: string }) =>
      id === undefined ? questionsApi.create(input) : questionsApi.update(id, input, version!),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}

/** Imported questions arrive Hidden; the lists reload so they can be checked (UC-55). */
export function useImportQuestions() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ subjectId, file }: { subjectId: number; file: File }) => questionsApi.importFile(subjectId, file),
    onSuccess: (result) => {
      if (result.imported > 0) void queryClient.invalidateQueries({ queryKey: keys.all })
    },
  })
}

export function useChangeQuestionStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: number; status: VisibilityStatus }) => questionsApi.changeStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}
