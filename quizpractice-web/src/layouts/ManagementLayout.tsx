import { useEffect, useRef, type ReactNode } from 'react'
import { App, Breadcrumb, Button, Flex, Layout, Menu, Result, Spin, Typography } from 'antd'
import { Link, Outlet, useLocation, useNavigate } from 'react-router'
import { AccountMenu } from '../components/account/AccountMenu'
import { useOpenAccountModal } from '../components/account/accountModalContext'
import { firstScreen, MENU_SCREENS, menuItemOf } from '../constants/menu'
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

/**
 * Frame of every Marketing, Sale, Expert and Admin screen (SRS III 0.1): system name,
 * role menu, account menu, breadcrumb; the screen fills the content area.
 *
 * The role menu is the one the API sends (D23). A screen whose menu item the role does
 * not have is refused here too, so typing its address shows a refusal instead of an
 * empty screen (BR-03). This only hides screens: the API checks every request again.
 * A guest never sees this frame: it goes back to the Home Page with the login pop-up.
 * Neither does a role without any management screen (the customer): back home.
 */
export function ManagementLayout() {
  const me = useCurrentUser()
  const { pathname } = useLocation()
  const user = me.data

  if (!me.isPending && !user) return <SignInFirst />
  if (user && !firstScreen(user.menu)) return <StaffOnly />

  // The menu item this address belongs to, e.g. /manage/posts/7 → Posts.
  const currentItem = menuItemOf(pathname)
  const menu = (user?.menu ?? []).filter((item) => MENU_SCREENS[item]?.area === 'manage')

  const segments = pathname.split('/').filter(Boolean)
  const crumbs = segments.map((s, i) => {
    const label = CRUMBS[s] ?? (/^\d+$/.test(s) ? `#${s}` : s)
    const to = '/' + segments.slice(0, i + 1).join('/')
    return { title: i < segments.length - 1 && i > 0 ? <Link to={to}>{label}</Link> : label }
  })

  let body: ReactNode = <Outlet />
  if (!user) body = <Spin style={{ display: 'block', padding: 48 }} />
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

/**
 * A Guest who typed a management address, or whose session ended: back to the Home
 * Page (replacing the address, so Back does not return here) with the login pop-up,
 * which opens the address after signing in if the role has it.
 */
function SignInFirst() {
  const navigate = useNavigate()
  const { pathname, search } = useLocation()
  const openModal = useOpenAccountModal()
  const { message } = App.useApp()
  const sent = useRef(false)

  useEffect(() => {
    if (sent.current) return
    sent.current = true
    navigate('/', { replace: true })
    message.info('Sign in to open that screen.')
    openModal('login', { signInTo: pathname + search })
  }, [navigate, pathname, search, openModal, message])

  return null
}

/** BR-03: a customer typed a management address; it has no management screen at all. */
function StaffOnly() {
  const navigate = useNavigate()
  const { message } = App.useApp()
  const sent = useRef(false)

  useEffect(() => {
    if (sent.current) return
    sent.current = true
    navigate('/', { replace: true })
    message.info('That screen is for staff.')
  }, [navigate, message])

  return null
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
