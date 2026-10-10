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

/** Slider Details form. image is a new file to upload; empty on edit keeps the stored one. */
export interface SliderInput {
  title: string
  backlink?: string
  status: VisibilityStatus
  notes?: string
  image?: File
}

function toFormData(input: SliderInput): FormData {
  const data = new FormData()
  data.append('title', input.title)
  data.append('status', input.status)
  if (input.backlink) data.append('backlink', input.backlink)
  if (input.notes) data.append('notes', input.notes)
  if (input.image) data.append('image', input.image)
  return data
}

/** /api/sliders — UC-38 View Sliders List, UC-39 Add or Edit Slider. */
export const slidersApi = {
  list: (criteria: SliderCriteria) =>
    http.get<Page<Slider>>('/sliders', { params: criteria }).then((r) => r.data),

  get: (id: number) => http.get<Slider>(`/sliders/${id}`).then((r) => r.data),

  create: (input: SliderInput) => http.post<Slider>('/sliders', toFormData(input)).then((r) => r.data),

  update: (id: number, input: SliderInput) =>
    http.put<Slider>(`/sliders/${id}`, toFormData(input)).then((r) => r.data),
}
