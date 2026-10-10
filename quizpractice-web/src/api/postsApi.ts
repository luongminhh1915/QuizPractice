import { http } from './client'
import type { ActiveStatus, ListCriteria, Page, VisibilityStatus } from './types'

/** Matches PostSummaryDto of the API: a row of Posts List, without the body. */
export interface PostSummary {
  id: number
  title: string
  thumbnail: string
  categoryId: number
  category: string
  isFeatured: boolean
  status: VisibilityStatus
  updatedAt: string
}

export interface PostCategory {
  id: number
  name: string
  status: ActiveStatus
}

export interface PostCriteria extends ListCriteria {
  categoryId?: number
  status?: VisibilityStatus
}

/** /api/posts — UC-35 View Posts List. */
export const postsApi = {
  list: (criteria: PostCriteria) =>
    http.get<Page<PostSummary>>('/posts', { params: criteria }).then((r) => r.data),

  categories: () => http.get<PostCategory[]>('/posts/categories').then((r) => r.data),
}
