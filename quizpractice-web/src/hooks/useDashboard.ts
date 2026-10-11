import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { dashboardApi, type DashboardRange } from '../api/dashboardApi'

/** The figures of UC-34 over a range; the previous figures stay on screen while the next load. */
export function useDashboard(range: DashboardRange) {
  return useQuery({
    queryKey: ['dashboard', range.from ?? null, range.to ?? null],
    queryFn: () => dashboardApi.get(range),
    placeholderData: keepPreviousData,
  })
}
