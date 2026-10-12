import { useState } from 'react'
import { Button } from 'antd'
import type { Price } from '../../api/publicApi'
import { useOpenAccountModal } from '../account/accountModalContext'
import { useCurrentUser } from '../../hooks/useAccount'
import { SubjectRegisterModal } from './SubjectRegisterModal'

/**
 * Register of SRS III 2.4 and 2.5. A customer opens Subject Register (UC-14). A guest is
 * asked to log in first and stays on the page, so Register works right after (UC-12 A5,
 * UC-13 A2; OI-15). Not shown to the other roles, and not for a subject without an
 * active package (UC-12 E2).
 */
export function RegisterSubjectButton({ subjectId, price, block }: { subjectId: number; price: Price | null; block?: boolean }) {
  const me = useCurrentUser()
  const openModal = useOpenAccountModal()
  const [open, setOpen] = useState(false)

  if (!price || me.isPending) return null
  const user = me.data
  if (user && user.role !== 'Customer') return null

  return (
    <>
      <Button type="primary" block={block} onClick={() => (user ? setOpen(true) : openModal('login'))}>
        Register
      </Button>
      {user && <SubjectRegisterModal target={{ subjectId }} open={open} onClose={() => setOpen(false)} />}
    </>
  )
}
