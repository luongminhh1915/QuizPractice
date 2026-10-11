import type { ReactNode } from 'react'
import { Breadcrumb, Button, Flex, Layout, Menu, Result, Spin, Typography } from 'antd'
import { Link, Outlet, useLocation } from 'react-router'
import { AccountMenu } from '../components/account/AccountMenu'
import { useOpenAccountModal } from '../components/account/accountModalContext'
import { firstScreen, MENU_SCREENS } from '../constants/menu'
import { useCurrentUser } from '../hooks/useAccount'

const { Header, Sider, Content } = Layout

// Breadcrumb labels by path segment; a number is a record id.
const CRUMBS: Record<string, string> = {
  manage: 'Management',
  dashboard: 'Dashboard',
  posts: 'Posts',
  sliders: 'Sliders',
  questions: 'Questions',
  quizzes: 'Quizzes',
  new: 'New',
}

const MANAGE_SCREENS = Object.entries(MENU_SCREENS).filter(([, s]) => s.area === 'manage')

/**
 * Frame of every Marketing, Sale, Expert and Admin screen (SRS III 0.1): system name,
 * role menu, account menu, breadcrumb; the screen fills the content area.
 *
 * The role menu is the one the API sends (D23). A screen whose menu item the role does
 * not have is refused here too, so typing its address shows a refusal instead of an
 * empty screen (BR-03). This only hides screens: the API checks every request again.
 */
export function ManagementLayout() {
  const me = useCurrentUser()
  const { pathname } = useLocation()
  const user = me.data

  // The menu item this address belongs to, e.g. /manage/posts/7 → Posts.
  const [currentItem] = MANAGE_SCREENS.find(([, s]) => pathname === s.path || pathname.startsWith(s.path + '/')) ?? []
  const menu = (user?.menu ?? []).filter((item) => MENU_SCREENS[item]?.area === 'manage')

  const segments = pathname.split('/').filter(Boolean)
  const crumbs = segments.map((s, i) => {
    const label = CRUMBS[s] ?? (/^\d+$/.test(s) ? `#${s}` : s)
    const to = '/' + segments.slice(0, i + 1).join('/')
    return { title: i < segments.length - 1 && i > 0 ? <Link to={to}>{label}</Link> : label }
  })

  let body: ReactNode = <Outlet />
  if (me.isPending) body = <Spin style={{ display: 'block', padding: 48 }} />
  else if (!user) body = <SignInFirst />
  else if (currentItem && !menu.includes(currentItem)) body = <NotForRole home={firstScreen(user.menu)} />

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider width={220} theme="light" style={{ borderRight: '1px solid #f0f0f0' }}>
        <Link to="/" style={{ display: 'block', padding: '18px 24px' }}>
          <Typography.Title level={4} style={{ margin: 0 }}>
            QuizPractice
          </Typography.Title>
        </Link>
        <Menu
          mode="inline"
          selectedKeys={currentItem ? [currentItem] : []}
          items={menu.map((item) => {
            const screen = MENU_SCREENS[item]
            return {
              key: item,
              icon: screen.icon,
              disabled: !screen.ready,
              label: screen.ready ? <Link to={screen.path}>{item}</Link> : `${item} (coming soon)`,
            }
          })}
          style={{ borderInlineEnd: 0 }}
        />
      </Sider>
      <Layout>
        <Header style={{ background: '#fff', padding: '0 24px', borderBottom: '1px solid #f0f0f0' }}>
          <Flex justify="space-between" align="center" style={{ height: '100%' }}>
            <Breadcrumb items={crumbs} />
            <AccountMenu area="manage" />
          </Flex>
        </Header>
        <Content style={{ margin: 24 }}>
          <div style={{ background: '#fff', padding: 24, borderRadius: 8 }}>{body}</div>
        </Content>
      </Layout>
    </Layout>
  )
}

/** A Guest who opened a management address, or whose session ended. */
function SignInFirst() {
  const openModal = useOpenAccountModal()
  return (
    <Result
      status="403"
      title="Sign in to open this screen"
      extra={
        <Button type="primary" onClick={() => openModal('login')}>
          Login
        </Button>
      }
    />
  )
}

/** BR-03: signed in, but the role does not have this screen. */
function NotForRole({ home }: { home?: string }) {
  return (
    <Result
      status="403"
      title="Your role cannot open this screen"
      extra={
        <Link to={home ?? '/'}>
          <Button type="primary">{home ? 'Back to your first screen' : 'Home Page'}</Button>
        </Link>
      }
    />
  )
}
