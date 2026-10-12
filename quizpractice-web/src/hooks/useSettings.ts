import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { settingsApi, type SettingCriteria, type SettingInput } from '../api/settingsApi'

// Query keys: ['settings'] covers every list and detail of Settings.
const keys = {
  all: ['settings'] as const,
  list: (criteria: SettingCriteria) => ['settings', 'list', criteria] as const,
  detail: (id: number) => ['settings', 'detail', id] as const,
}

export function useSettingList(criteria: SettingCriteria) {
  return useQuery({
    queryKey: keys.list(criteria),
    queryFn: () => settingsApi.list(criteria),
    placeholderData: keepPreviousData,
  })
}

export function useSetting(id: number | undefined) {
  return useQuery({ queryKey: keys.detail(id ?? 0), queryFn: () => settingsApi.get(id!), enabled: id !== undefined })
}

/**
 * UC-65: every screen reads settings (menu, categories, levels...), so after a change the
 * whole cache reloads rather than guessing which screens follow the type.
 */
export function useSaveSetting(id: number | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: SettingInput & { version?: string; confirmInUse?: boolean }) =>
      id === undefined ? settingsApi.create(input) : settingsApi.update(id, { ...input, version: input.version ?? '' }),
    onSuccess: () => queryClient.invalidateQueries(),
  })
}
