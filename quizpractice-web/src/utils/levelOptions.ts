import type { QuestionLevel } from '../api/questionsApi'

/**
 * Level choices of a details form: the levels in use, plus the current level of the
 * record when it has been switched off since, marked so (the API accepts that one only
 * for the same record, "Choose a level in use" otherwise).
 */
export function levelOptions(levels: QuestionLevel[], currentId?: number) {
  return levels
    .filter((l) => l.active || l.id === currentId)
    .map((l) => ({ value: l.id, label: l.active ? l.name : `${l.name} (not in use)` }))
}
