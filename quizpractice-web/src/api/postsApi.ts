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

/** Matches PostDto of the API. */
export interface Post extends PostSummary {
  brief: string
  content: string
  authorId: number
  author: string
  /** Sent back on Save: the API refuses it when another user saved in between (UC-36 E3). */
  version: number
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

/** Post Details form. thumbnail is a new file to upload; empty on edit keeps the stored one. */
export interface PostInput {
  title: string
  categoryId: number
  brief: string
  content?: string
  isFeatured: boolean
  status: VisibilityStatus
  thumbnail?: File
}

function toFormData(input: PostInput, version?: number): FormData {
  const data = new FormData()
  data.append('title', input.title)
  data.append('categoryId', String(input.categoryId))
  data.append('brief', input.brief)
  data.append('content', input.content ?? '')
  data.append('isFeatured', String(input.isFeatured))
  data.append('status', input.status)
  if (version !== undefined) data.append('version', String(version))
  if (input.thumbnail) data.append('thumbnail', input.thumbnail)
  return data
}

/** /api/posts — UC-35 View Posts List, UC-36 Add or Edit Post. */
export const postsApi = {
  list: (criteria: PostCriteria) =>
    http.get<Page<PostSummary>>('/posts', { params: criteria }).then((r) => r.data),

  get: (id: number) => http.get<Post>(`/posts/${id}`).then((r) => r.data),

  categories: () => http.get<PostCategory[]>('/posts/categories').then((r) => r.data),

  create: (input: PostInput) => http.post<Post>('/posts', toFormData(input)).then((r) => r.data),

  update: (id: number, input: PostInput, version: number) =>
    http.put<Post>(`/posts/${id}`, toFormData(input, version)).then((r) => r.data),

  /** An image for the body; answers the address the editor inserts. */
  uploadContentImage: (image: File) => {
    const data = new FormData()
    data.append('image', image)
    return http.post<{ url: string }>('/posts/content-images', data).then((r) => r.data.url)
  },
}
