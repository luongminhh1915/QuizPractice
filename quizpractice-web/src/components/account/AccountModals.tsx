import { useCallback, useState, type ReactNode } from 'react'
import { AccountModalsContext, type AccountModal, type AccountModalOptions } from './accountModalContext'
import { ChangePasswordModal } from './ChangePasswordModal'
import { LoginModal } from './LoginModal'
import { ProfileModal } from './ProfileModal'
import { RegisterModal } from './RegisterModal'

/**
 * Holds the account pop-ups (SRS III 0.5: a pop-up over the page it was opened from).
 * One at a time: opening another replaces it, as Register replaces Login (UC-01 A1).
 */
export function AccountModals({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState<{ modal: AccountModal } & AccountModalOptions>()
  const openModal = useCallback(
    (modal: AccountModal, options?: AccountModalOptions) => setOpen({ modal, ...options }),
    [],
  )
  const close = () => setOpen(undefined)

  return (
    <AccountModalsContext.Provider value={openModal}>
      {children}
      <LoginModal open={open?.modal === 'login'} signInTo={open?.signInTo} onClose={close} />
      <RegisterModal open={open?.modal === 'register'} onClose={close} />
      <ProfileModal open={open?.modal === 'profile'} onClose={close} />
      <ChangePasswordModal open={open?.modal === 'password'} onClose={close} />
    </AccountModalsContext.Provider>
  )
}
