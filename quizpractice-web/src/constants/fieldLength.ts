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
