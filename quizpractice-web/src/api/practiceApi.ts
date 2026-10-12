import { http } from './client'

export type SelectionType = 'Topic' | 'Dimension'

/** Matches PracticeGroupDto: a topic, or a dimension with its type. */
export interface PracticeGroup {
  id: number
  name: string
  type: string | null
  available: number
}

/** Matches PracticeGroupsDto. total = the shown questions of the whole subject (all groups). */
export interface PracticeGroups {
  total: number
  groups: PracticeGroup[]
}

export interface CreatePractice {
  subjectId: number
  questionCount: number
  selectionType: SelectionType
  /** Empty = all topics, or all dimensions. */
  groupId?: number
}

/** /api/practices — Practices List and Practice Details (F#14, F#15). */
export const practiceApi = {
  /** UC-20 step 2: the subjects the customer may practise; empty is E1. */
  form: () => http.get<{ subjects: { id: number; name: string }[] }>('/practices/form').then((r) => r.data),

  /** UC-20 step 3. */
  groups: (subjectId: number, selectedBy: SelectionType) =>
    http.get<PracticeGroups>('/practices/groups', { params: { subjectId, selectedBy } }).then((r) => r.data),

  /** UC-20: the attempt Quiz Handle opens. 400 QuestionCount when fewer match (E2). */
  create: (practice: CreatePractice) => http.post<{ id: number }>('/practices', practice).then((r) => r.data),
}
