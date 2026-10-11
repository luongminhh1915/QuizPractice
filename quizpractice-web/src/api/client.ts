import axios, { AxiosError } from 'axios'
import type { FormInstance } from 'antd'

/**
 * The one axios instance every xxxApi.ts uses (SDS I.1, api/). Paths are relative to
 * /api, which the Vite server forwards to the API in development.
 *
 * The sign-in cookie travels by itself (same origin). axios copies the XSRF-TOKEN
 * cookie, which GET /api/account/me hands out, into the X-XSRF-TOKEN header of every
 * request, so no screen has to remember the antiforgery check (CookieSignIn.cs).
 */
export const http = axios.create({
  baseURL: '/api',
  xsrfCookieName: 'XSRF-TOKEN',
  xsrfHeaderName: 'X-XSRF-TOKEN',
})

/** ProblemDetails as the API answers every error (ApiControllerBase). */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  code?: string
  /** Only on 400: field name → messages, e.g. { "Backlink": ["The backlink must…"] }. */
  errors?: Record<string, string[]>
}

export function problemOf(error: unknown): ProblemDetails | undefined {
  return error instanceof AxiosError ? (error.response?.data as ProblemDetails | undefined) : undefined
}

/** A short sentence for the message area (SRS III 0.1) whatever went wrong. */
export function errorMessage(error: unknown): string {
  const problem = problemOf(error)
  if (problem?.detail) return problem.detail
  if (problem?.title) return problem.title
  if (error instanceof AxiosError && !error.response) return 'The server cannot be reached. Try again in a moment.'
  return 'Something went wrong. Try again in a moment.'
}

/**
 * Puts the field errors of a 400 answer under the matching form fields (SRS III 0.4:
 * "its message is shown under it"). The API names fields like the C# request:
 * "Title", or "Image.Content" for a rule on part of a field; the form uses the first
 * segment in camelCase ("title", "image").
 *
 * Returns false when the error has no field to show it on, so the caller shows it in
 * the message area instead.
 */
export function applyFieldErrors(form: FormInstance, error: unknown): boolean {
  const fieldErrors = problemOf(error)?.errors
  if (!fieldErrors) return false

  // The fields the form has on screen, including empty ones such as an image not chosen.
  const known = new Set(form.getFieldsError().map((f) => f.name.join('.')))
  const byField = new Map<string, string[]>()
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const head = key.split('.')[0]
    const name = head.charAt(0).toLowerCase() + head.slice(1)
    byField.set(name, [...(byField.get(name) ?? []), ...messages])
  }

  const shown = [...byField].filter(([name]) => known.has(name))
  form.setFields(shown.map(([name, errors]) => ({ name, errors })))
  return shown.length === byField.size
}
