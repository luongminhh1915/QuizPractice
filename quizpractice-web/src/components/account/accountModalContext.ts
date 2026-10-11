import { createContext, useContext } from 'react'

/** The account pop-ups of SRS III 1.1 to 1.5; any page or menu can open one. */
export type AccountModal = 'login' | 'register' | 'profile'

export const AccountModalsContext = createContext<(modal: AccountModal) => void>(() => {})

/** Opens an account pop-up, e.g. useOpenAccountModal()('login'). */
export function useOpenAccountModal() {
  return useContext(AccountModalsContext)
}
