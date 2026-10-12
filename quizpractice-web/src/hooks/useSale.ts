import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { saleApi, type RegistrationInput, type RegistrationListCriteria } from '../api/saleApi'

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

/** UC-60 step 2. Not retried, so a 404 shows at once. */
export function useRegistration(id: number | undefined) {
  return useQuery({
    queryKey: [...saleKeys.registrations, 'detail', id ?? 0],
    queryFn: () => saleApi.registration(id!),
    enabled: id !== undefined,
    retry: false,
  })
}

/** UC-60 step 4. */
export function useRegistrationChoices() {
  return useQuery({ queryKey: [...saleKeys.registrations, 'choices'], queryFn: saleApi.registrationChoices })
}

/**
 * UC-60 step 7: a new registration, the whole form of one the Sale user may edit, or the
 * notes alone (BR-22). Whatever the answer, the list and the details reload.
 */
export function useSaveRegistration(id: number | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ input, notesOnly }: { input: RegistrationInput; notesOnly: boolean }) =>
      id === undefined
        ? saleApi.createRegistration(input)
        : notesOnly
          ? saleApi.saveRegistrationNotes(id, input.notes)
          : saleApi.updateRegistration(id, input),
    onSettled: () => queryClient.invalidateQueries({ queryKey: saleKeys.registrations }),
  })
}

/** UC-59: the Subject filter. */
export function useRegistrationFilters() {
  return useQuery({ queryKey: [...saleKeys.registrations, 'filters'], queryFn: saleApi.registrationFilters })
}
