import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { settingsApi, type SettingCriteria } from '../api/settingsApi'

// Query keys: ['settings'] covers every list and detail of Settings.
const keys = {
  all: ['settings'] as const,
  list: (criteria: SettingCriteria) => ['settings', 'list', criteria] as const,
}

export function useSettingList(criteria: SettingCriteria) {
  return useQuery({
    queryKey: keys.list(criteria),
    queryFn: () => settingsApi.list(criteria),
    placeholderData: keepPreviousData,
  })
}
