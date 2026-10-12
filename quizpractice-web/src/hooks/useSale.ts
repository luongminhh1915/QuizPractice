import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { saleApi, type RegistrationListCriteria } from '../api/saleApi'

export const saleKeys = {
  registrations: ['sale', 'registrations'] as const,
}

/** UC-59. */
export function useRegistrationList(criteria: RegistrationListCriteria) {
  return useQuery({
    queryKey: [...saleKeys.registrations, 'list', criteria],
    queryFn: () => saleApi.registrations(criteria),
    placeholderData: keepPreviousData,
  })
}

/** UC-59: the Subject filter. */
export function useRegistrationFilters() {
  return useQuery({ queryKey: [...saleKeys.registrations, 'filters'], queryFn: saleApi.registrationFilters })
}
