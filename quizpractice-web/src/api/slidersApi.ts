import { http } from './client'
import type { ListCriteria, Page, VisibilityStatus } from './types'

/** Matches SliderDto of the API. */
export interface Slider {
  id: number
  title: string
  image: string
  backlink: string | null
  displayOrder: number
  notes: string | null
  status: VisibilityStatus
}

export interface SliderCriteria extends ListCriteria {
  status?: VisibilityStatus
}

/** /api/sliders — UC-38 View Sliders List. */
export const slidersApi = {
  list: (criteria: SliderCriteria) =>
    http.get<Page<Slider>>('/sliders', { params: criteria }).then((r) => r.data),
}
