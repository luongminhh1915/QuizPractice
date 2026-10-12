import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { attemptApi } from '../api/attemptApi'

export const attemptKeys = {
  handle: (id: number) => ['account', 'attempts', id, 'handle'] as const,
  review: (id: number) => ['account', 'attempts', id, 'review'] as const,
}

/**
 * UC-24, UC-30: the running attempt. Read once: the page keeps answers and position
 * itself and stores each change, so a refetch on focus would only fight with it.
 */
export function useQuizHandle(id: number) {
  return useQuery({
    queryKey: attemptKeys.handle(id),
    queryFn: () => attemptApi.get(id),
    retry: false,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
    gcTime: 0,
  })
}

/** UC-31: a scored attempt never changes, so it is read once. */
export function useQuizReview(id: number) {
  return useQuery({ queryKey: attemptKeys.review(id), queryFn: () => attemptApi.review(id), retry: false, staleTime: Infinity })
}

/** UC-25: stores the question on screen. */
export function useMove(id: number) {
  return useMutation({ mutationFn: (order: number) => attemptApi.move(id, order) })
}

/** UC-26. */
export function useMark(id: number) {
  return useMutation({
    mutationFn: ({ order, marked }: { order: number; marked: boolean }) => attemptApi.mark(id, order, marked),
  })
}

/** UC-28: read when the customer asks, never before (the answer stays on the server). */
export function usePeek(id: number) {
  return useMutation({ mutationFn: (order: number) => attemptApi.peek(id, order) })
}

/** UC-29: the lists and details of the customer change with the result. */
export function useSubmit(id: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => attemptApi.submit(id),
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['account', 'practices'] }),
  })
}

/** UC-24 step 3. */
export function useAnswer(id: number) {
  return useMutation({
    mutationFn: ({ order, optionId }: { order: number; optionId: number }) => attemptApi.answer(id, order, optionId),
  })
}
