import { http } from './client'
import type { Page } from './types'

/** Matches PostCardDto: a shown post as a card or a sider line. */
export interface PostCard {
  id: number
  title: string
  thumbnail: string
  brief: string
  category: string
  updatedAt: string
}

/** Matches PriceDto: the active package with the lowest sale price. */
export interface Price {
  packageId: number
  packageName: string
  accessMonths: number
  listPrice: number
  salePrice: number
}

/** Matches SubjectCardDto: a published subject as a card or a sider line. */
export interface SubjectCard {
  id: number
  name: string
  thumbnail: string
  tagline: string | null
  category: string
  /** Null when the subject has no active package: no price and no Register (UC-12 E2). */
  lowestPrice: Price | null
}

/** A post or subject category of the system settings (BR-16). */
export interface Category {
  id: number
  name: string
}

/** Criteria of Blogs List (UC-10). */
export interface BlogCriteria {
  page: number
  pageSize: number
  search?: string
  categoryId?: number
}

/** Matches BlogSiderDto. */
export interface BlogSider {
  categories: Category[]
  latestPosts: PostCard[]
}

/** Matches HomeDto (UC-09). */
export interface Home {
  hotPosts: PostCard[]
  featuredSubjects: SubjectCard[]
  latestPosts: PostCard[]
}

/** /api/public — pages open to everyone (Public feature). */
export const publicApi = {
  /** UC-09: the blocks of the Home Page besides the sliders. */
  home: () => http.get<Home>('/public/home').then((r) => r.data),

  /** UC-10: a page of shown posts. */
  blogs: (criteria: BlogCriteria) => http.get<Page<PostCard>>('/public/blogs', { params: criteria }).then((r) => r.data),

  /** The sider of both blog pages. */
  blogSider: () => http.get<BlogSider>('/public/blogs/sider').then((r) => r.data),
}
