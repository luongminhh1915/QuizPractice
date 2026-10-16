import { createContext, useContext } from 'react'

/** The account pop-ups of SRS III 1.1 to 1.5; any page or menu can open one. */
export type AccountModal = 'login' | 'register' | 'profile' | 'password'

/**
 * signInTo: the management address a guest typed (ManagementLayout sends the guest to
 * the Home Page). The login pop-up opens it after signing in when the role has it; it
 * is forgotten when the pop-up closes.
 */
export interface AccountModalOptions {
  signInTo?: string
}

export const AccountModalsContext = createContext<(modal: AccountModal, options?: AccountModalOptions) => void>(
  () => {},
)

/** Opens an account pop-up, e.g. useOpenAccountModal()('login'). */
export function useOpenAccountModal() {
  return useContext(AccountModalsContext)
}
