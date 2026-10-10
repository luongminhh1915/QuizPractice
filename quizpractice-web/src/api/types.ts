/** One page of a list screen, as Page<T> of the API returns it. */
export interface Page<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

/** Criteria every list screen shares (SRS III 0.3). */
export interface ListCriteria {
  page: number
  pageSize: number
  search?: string
  sortBy?: string
  descending?: boolean
}

/** Post, Slider, Question (BR-23). */
export type VisibilityStatus = 'Shown' | 'Hidden'

/** Setting, PricePackage, Lesson, Quiz (BR-23). */
export type ActiveStatus = 'Active' | 'Inactive'
