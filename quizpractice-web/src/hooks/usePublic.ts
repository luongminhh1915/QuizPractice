import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { publicApi, type BlogCriteria } from '../api/publicApi'

// ['public', ...] holds what the public pages show. It changes only when a Marketing
// or Admin user edits something, so a minute without reloading is fine.
const STALE = 60 * 1000

/** UC-09. */
export function useHome() {
  return useQuery({ queryKey: ['public', 'home'], queryFn: publicApi.home, staleTime: STALE })
}

/** UC-10. */
export function useBlogs(criteria: BlogCriteria) {
  return useQuery({
    queryKey: ['public', 'blogs', criteria],
    queryFn: () => publicApi.blogs(criteria),
    staleTime: STALE,
    placeholderData: keepPreviousData,
  })
}

export function useBlogSider() {
  return useQuery({ queryKey: ['public', 'blogs', 'sider'], queryFn: publicApi.blogSider, staleTime: STALE })
}
