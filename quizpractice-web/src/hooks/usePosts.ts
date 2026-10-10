import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { postsApi, type PostCriteria } from '../api/postsApi'

// Query keys: ['posts'] covers every list and detail, so one invalidate after a
// change reloads whatever post screen is open.
const keys = {
  all: ['posts'] as const,
  list: (criteria: PostCriteria) => ['posts', 'list', criteria] as const,
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

/** Post Category settings (BR-16), for the filter and the form. */
export function usePostCategories() {
  return useQuery({ queryKey: keys.categories, queryFn: postsApi.categories, staleTime: 5 * 60_000 })
}
