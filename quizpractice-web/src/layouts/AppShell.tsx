import { Outlet } from 'react-router'
import { AccountModals } from '../components/account/AccountModals'

/**
 * Root of every route: the account pop-ups sit here, so the header of either layout
 * and any page (e.g. Reset Password offering the login pop-up) can open them.
 */
export function AppShell() {
  return (
    <AccountModals>
      <Outlet />
    </AccountModals>
  )
}
