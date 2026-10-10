import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { postsApi, type PostCriteria, type PostInput } from '../api/postsApi'
import type { VisibilityStatus } from '../api/types'

// Query keys: ['posts'] covers every list and detail, so one invalidate after a
// change reloads whatever post screen is open.
const keys = {
  all: ['posts'] as const,
  list: (criteria: PostCriteria) => ['posts', 'list', criteria] as const,
  detail: (id: number) => ['posts', 'detail', id] as const,
  categories: ['posts', 'categories'] as const,
}

export function usePostList(criteria: PostCriteria) {
  return useQuery({
    queryKey: keys.list(criteria),
    queryFn: () => postsApi.list(criteria),
    // Keep the current rows on screen while the next page or sort loads.
    placeholderData: keepPreviousData,
  })
}

export function usePost(id: number | undefined) {
  return useQuery({
    queryKey: keys.detail(id ?? 0),
    queryFn: () => postsApi.get(id!),
    enabled: id !== undefined,
  })
}

/** Post Category settings (BR-16), for the filter and the form. */
export function usePostCategories() {
  return useQuery({ queryKey: keys.categories, queryFn: postsApi.categories, staleTime: 5 * 60_000 })
}

/** version: the one the post was loaded with; required on edit (UC-36 E3). */
export function useSavePost(id: number | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ input, version }: { input: PostInput; version?: number }) =>
      id === undefined ? postsApi.create(input) : postsApi.update(id, input, version!),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}

export function useChangePostStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: number; status: VisibilityStatus }) => postsApi.changeStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}
