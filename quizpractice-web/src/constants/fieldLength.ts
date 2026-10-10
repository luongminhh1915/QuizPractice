// Limits of BR-17 (OI-11 pending), the same numbers as Domain/Constants/FieldLength.cs.
// The API is the authority; these only let the form answer before the request is sent.
// Change both files together.

export const NAME_MAX = 200
export const BRIEF_MAX = 500
export const URL_MAX = 500

export const IMAGE_MAX_BYTES = 2 * 1024 * 1024
export const IMAGE_TYPES = ['image/jpeg', 'image/png']

/** Description, post content, question content, explanation, HTML lesson. */
export const LONG_TEXT_MAX = 20_000

export const ANSWER_OPTION_MAX = 1_000
export const MIN_OPTIONS = 2
export const MAX_OPTIONS = 6

/** Question media (UC-53 E3; Duck 10/10), as QuestionMedia.Types in Application/Common. */
export const QUESTION_MEDIA = [
  { kind: 'image', types: ['image/jpeg', 'image/png', 'image/webp'], maxBytes: 2 * 1024 * 1024 },
  { kind: 'audio', types: ['audio/mpeg'], maxBytes: 10 * 1024 * 1024 },
  { kind: 'video', types: ['video/mp4'], maxBytes: 20 * 1024 * 1024 },
] as const
