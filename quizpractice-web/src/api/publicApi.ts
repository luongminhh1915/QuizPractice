import { http } from './client'

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
}
