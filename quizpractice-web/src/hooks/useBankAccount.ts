import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { bankAccountApi, type BankAccountInput } from '../api/bankAccountApi'

const keys = {
  account: ['bank-account'] as const,
  sample: (bin: string, number: string) => ['bank-account', 'sample', bin, number] as const,
}

export function useBankAccount() {
  return useQuery({ queryKey: keys.account, queryFn: bankAccountApi.get })
}

export function useSaveBankAccount() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ input, version }: { input: BankAccountInput; version: string | null }) => bankAccountApi.save(input, version),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.account }),
  })
}

/** Step 6: redrawn when the bank or the number changes; only for a number that can be one. */
export function useSampleQr(bin: string | undefined, number: string | undefined) {
  const valid = !!bin && !!number && /^[0-9]{6,19}$/.test(number)
  return useQuery({
    queryKey: keys.sample(bin ?? '', number ?? ''),
    queryFn: () => bankAccountApi.sampleQr(bin!, number!),
    enabled: valid,
    placeholderData: keepPreviousData,
    staleTime: Infinity,
  })
}
