import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { publicApi, type BlogCriteria, type SubjectCriteria } from '../api/publicApi'

// ['public', ...] holds what the public pages show. It changes only when a Marketing
// or Admin user edits something, so a minute without reloading is fine.
const STALE = 60 * 1000

/** UC-09. */
export function useHome() {
  return useQuery({ queryKey: ['public', 'home'], queryFn: publicApi.home, staleTime: STALE })
}

/** UC-12. */
export function useSubjects(criteria: SubjectCriteria) {
  return useQuery({
    queryKey: ['public', 'subjects', criteria],
    queryFn: () => publicApi.subjects(criteria),
    staleTime: STALE,
    placeholderData: keepPreviousData,
  })
}

export function useSubjectSider() {
  return useQuery({ queryKey: ['public', 'subjects', 'sider'], queryFn: publicApi.subjectSider, staleTime: STALE })
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

/** UC-11. Not retried: a 404 sends the reader back to the list at once (E1). */
export function useBlogPost(id: number) {
  return useQuery({ queryKey: ['public', 'blogs', 'post', id], queryFn: () => publicApi.blogPost(id), staleTime: STALE, retry: false })
}

export function useBlogSider() {
  return useQuery({ queryKey: ['public', 'blogs', 'sider'], queryFn: publicApi.blogSider, staleTime: STALE })
}
