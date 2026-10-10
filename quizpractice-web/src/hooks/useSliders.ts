import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { slidersApi, type SliderCriteria, type SliderInput } from '../api/slidersApi'

// Query keys: ['sliders'] covers every list and detail, so one invalidate after a
// change reloads whatever slider screen is open.
const keys = {
  all: ['sliders'] as const,
  list: (criteria: SliderCriteria) => ['sliders', 'list', criteria] as const,
  detail: (id: number) => ['sliders', 'detail', id] as const,
}

export function useSliderList(criteria: SliderCriteria) {
  return useQuery({
    queryKey: keys.list(criteria),
    queryFn: () => slidersApi.list(criteria),
    // Keep the current rows on screen while the next page or sort loads.
    placeholderData: keepPreviousData,
  })
}

export function useSlider(id: number | undefined) {
  return useQuery({
    queryKey: keys.detail(id ?? 0),
    queryFn: () => slidersApi.get(id!),
    enabled: id !== undefined,
  })
}

export function useSaveSlider(id: number | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: SliderInput) => (id === undefined ? slidersApi.create(input) : slidersApi.update(id, input)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}
