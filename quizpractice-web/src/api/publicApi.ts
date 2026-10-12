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

/** Matches SubjectDetailsDto (UC-13). description is HTML cleaned by the API. */
export interface SubjectDetails {
  id: number
  name: string
  tagline: string | null
  thumbnail: string
  brief: string
  description: string
  categoryId: number
  category: string
  lowestPrice: Price | null
}

/** Criteria of Subjects List (UC-12). */
export interface SubjectCriteria {
  page: number
  pageSize: number
  search?: string
  categoryId?: number
}

/** Matches SubjectSiderDto. */
export interface SubjectSider {
  categories: Category[]
  featuredSubjects: SubjectCard[]
}

/** Matches BlogSiderDto. */
export interface BlogSider {
  categories: Category[]
  latestPosts: PostCard[]
}

/** Matches BlogPostDto (UC-11). content is HTML cleaned by the API when saved. */
export interface BlogPost {
  id: number
  title: string
  thumbnail: string
  brief: string
  content: string
  categoryId: number
  category: string
  author: string
  updatedAt: string
}

/** Matches SubjectRegisterFormDto (UC-14). */
export interface RegisterForm {
  subjectId: number
  subjectName: string
  packages: { id: number; name: string; accessMonths: number; listPrice: number; salePrice: number; description: string | null }[]
  contact: { fullName: string; email: string; mobile: string; gender: string }
  /** Profile fields that block Submit, by field (E1). */
  contactProblems: Record<string, string[]>
  /** A3: the Submitted registration the account already has for the subject. */
  existing: { id: number; packageName: string; totalCost: number; registeredAt: string } | null
  /** UC-16: the stored package when the pop-up changes a registration. */
  selectedPackageId: number | null
}

/** Matches SubjectRegisteredDto (UC-14). */
export interface SubjectRegistered {
  id: number
  subjectName: string
  packageName: string
  totalCost: number
  status: string
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

  /** UC-12: a page of published subjects, each with its cheapest active package. */
  subjects: (criteria: SubjectCriteria) =>
    http.get<Page<SubjectCard>>('/public/subjects', { params: criteria }).then((r) => r.data),

  /** UC-13. 404 when the subject does not exist or is not published (E1). */
  subject: (id: number) => http.get<SubjectDetails>(`/public/subjects/${id}`).then((r) => r.data),

  /** The sider of both subject pages. */
  subjectSider: () => http.get<SubjectSider>('/public/subjects/sider').then((r) => r.data),

  /** UC-14, customer only: packages, contact from the profile, what blocks a registration. */
  registerForm: (subjectId: number) => http.get<RegisterForm>(`/subjects/${subjectId}/registration`).then((r) => r.data),

  /** UC-14. 409 for A3 and E2, 400 Profile.Incomplete for E1. */
  register: (subjectId: number, packageId: number) =>
    http.post<SubjectRegistered>(`/subjects/${subjectId}/registration`, { packageId }).then((r) => r.data),

  /** UC-10: a page of shown posts. */
  blogs: (criteria: BlogCriteria) => http.get<Page<PostCard>>('/public/blogs', { params: criteria }).then((r) => r.data),

  /** UC-11. 404 when the post does not exist or is hidden (E1). */
  blogPost: (id: number) => http.get<BlogPost>(`/public/blogs/${id}`).then((r) => r.data),

  /** The sider of both blog pages. */
  blogSider: () => http.get<BlogSider>('/public/blogs/sider').then((r) => r.data),
}
