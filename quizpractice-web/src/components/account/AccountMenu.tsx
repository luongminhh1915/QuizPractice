import { App, Avatar, Button, Dropdown, Flex, Space, type MenuProps } from 'antd'
import { AppstoreOutlined, LogoutOutlined, UserOutlined } from '@ant-design/icons'
import { Link, useNavigate } from 'react-router'
import { firstScreen } from '../../constants/menu'
import { useCurrentUser, useLogout } from '../../hooks/useAccount'
import { useOpenAccountModal } from './accountModalContext'

/**
 * Account Menu of SRS III 0.1 and 0.2: email and role of the signed-in user and the
 * account actions. A Guest sees Login here instead. On a public page, a management
 * role also gets a link back to its first screen.
 */
export function AccountMenu({ area }: { area: 'public' | 'manage' }) {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const me = useCurrentUser()
  const openModal = useOpenAccountModal()
  const logout = useLogout()

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
    { type: 'divider' },
    { key: 'logout', icon: <LogoutOutlined />, label: 'Logout' },
  ]

  const onMenuClick: MenuProps['onClick'] = ({ key }) => {
    // UC-08: back to the public home page with the guest menu.
    if (key === 'logout') {
      logout.mutate(undefined, {
        onSettled: () => {
          navigate('/')
          message.success('You have signed out.')
        },
      })
    }
  }

  const label = (
    <Flex gap={8} align="center" style={{ cursor: 'pointer' }}>
      <Avatar size="small" src={user.avatar ?? undefined} icon={<UserOutlined />} />
      <span>
        {user.email} · {user.role}
      </span>
    </Flex>
  )

  return <Dropdown menu={{ items, onClick: onMenuClick }}>{label}</Dropdown>
}
