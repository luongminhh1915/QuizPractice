import { useQuery } from '@tanstack/react-query'
import { publicApi } from '../api/publicApi'

// ['public', ...] holds what the public pages show. It changes only when a Marketing
// or Admin user edits something, so a minute without reloading is fine.
const STALE = 60 * 1000

/** UC-09. */
export function useHome() {
  return useQuery({ queryKey: ['public', 'home'], queryFn: publicApi.home, staleTime: STALE })
}
