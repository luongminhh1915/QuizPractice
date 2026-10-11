import { useState, type ReactNode } from 'react'
import { AccountModalsContext, type AccountModal } from './accountModalContext'
import { LoginModal } from './LoginModal'
import { ProfileModal } from './ProfileModal'
import { RegisterModal } from './RegisterModal'

/**
 * Holds the account pop-ups (SRS III 0.5: a pop-up over the page it was opened from).
 * One at a time: opening another replaces it, as Register replaces Login (UC-01 A1).
 */
export function AccountModals({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState<AccountModal>()
  const close = () => setOpen(undefined)

  return (
    <AccountModalsContext.Provider value={setOpen}>
      {children}
      <LoginModal open={open === 'login'} onClose={close} />
      <RegisterModal open={open === 'register'} onClose={close} />
      <ProfileModal open={open === 'profile'} onClose={close} />
    </AccountModalsContext.Provider>
  )
}
