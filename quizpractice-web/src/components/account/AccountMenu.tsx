import { Avatar, Button, Dropdown, Flex, Space, type MenuProps } from 'antd'
import { AppstoreOutlined, UserOutlined } from '@ant-design/icons'
import { Link } from 'react-router'
import { firstScreen } from '../../constants/menu'
import { useCurrentUser } from '../../hooks/useAccount'
import { useOpenAccountModal } from './accountModalContext'

/**
 * Account Menu of SRS III 0.1 and 0.2: email and role of the signed-in user and the
 * account actions. A Guest sees Login here instead. On a public page, a management
 * role also gets a link back to its first screen.
 */
export function AccountMenu({ area }: { area: 'public' | 'manage' }) {
  const me = useCurrentUser()
  const openModal = useOpenAccountModal()

  if (me.isPending) return null

  const user = me.data
  if (!user) {
    return (
      <Space>
        <Button onClick={() => openModal('login')}>Login</Button>
      </Space>
    )
  }

  const home = firstScreen(user.menu)
  const items: MenuProps['items'] = [
    ...(area === 'public' && home
      ? [{ key: 'manage', icon: <AppstoreOutlined />, label: <Link to={home}>Management</Link> }]
      : []),
  ]

  const label = (
    <Flex gap={8} align="center" style={{ cursor: items.length ? 'pointer' : 'default' }}>
      <Avatar size="small" src={user.avatar ?? undefined} icon={<UserOutlined />} />
      <span>
        {user.email} · {user.role}
      </span>
    </Flex>
  )

  return items.length ? <Dropdown menu={{ items }}>{label}</Dropdown> : label
}
