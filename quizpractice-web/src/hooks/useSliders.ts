import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { slidersApi, type SliderCriteria } from '../api/slidersApi'

// Query keys: ['sliders'] covers every list and detail, so one invalidate after a
// change reloads whatever slider screen is open.
const keys = {
  all: ['sliders'] as const,
  list: (criteria: SliderCriteria) => ['sliders', 'list', criteria] as const,
}

export function useSliderList(criteria: SliderCriteria) {
  return useQuery({
    queryKey: keys.list(criteria),
    queryFn: () => slidersApi.list(criteria),
    // Keep the current rows on screen while the next page or sort loads.
    placeholderData: keepPreviousData,
  })
}
