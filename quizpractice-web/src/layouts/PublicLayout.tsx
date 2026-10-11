import { Flex, Layout, Menu, Typography } from 'antd'
import { Link, Outlet, useLocation } from 'react-router'
import { AccountMenu } from '../components/account/AccountMenu'
import { useCurrentUser } from '../hooks/useAccount'

const { Header, Content, Footer } = Layout

const PUBLIC_MENU = [
  { key: '/', label: 'Home' },
  { key: '/subjects', label: 'Subjects' },
  { key: '/blogs', label: 'Blogs' },
]

/**
 * Frame of the public pages and the customer pages (SRS III 0.2): public menu, account
 * area, content, footer. A page that needs the sider places it inside its own content
 * (see PublicSider), because the blocks differ per page.
 */
export function PublicLayout() {
  // Loads who is signed in and the antiforgery token every change needs.
  useCurrentUser()
  const { pathname } = useLocation()
  const selected = PUBLIC_MENU.filter((m) => (m.key === '/' ? pathname === '/' : pathname.startsWith(m.key)))

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ background: '#fff', borderBottom: '1px solid #f0f0f0', padding: '0 32px' }}>
        <Flex align="center" gap={32} style={{ height: '100%' }}>
          <Link to="/">
            <Typography.Title level={4} style={{ margin: 0 }}>
              QuizPractice
            </Typography.Title>
          </Link>
          <Menu
            mode="horizontal"
            selectedKeys={selected.map((m) => m.key)}
            items={PUBLIC_MENU.map((m) => ({ key: m.key, label: <Link to={m.key}>{m.label}</Link> }))}
            style={{ flex: 1, borderBottom: 0 }}
          />
          <AccountMenu area="public" />
        </Flex>
      </Header>
      <Content style={{ padding: '24px 32px', maxWidth: 1200, width: '100%', margin: '0 auto' }}>
        <Outlet />
      </Content>
      <Footer style={{ textAlign: 'center' }}>QuizPractice — SWP391 Group 2</Footer>
    </Layout>
  )
}

/** Sider of a public page: the content on the left, the blocks on the right. */
export function PublicSider({ children, blocks }: { children: React.ReactNode; blocks: React.ReactNode }) {
  return (
    <Flex gap={24} align="start">
      <div style={{ flex: 1, minWidth: 0 }}>{children}</div>
      <Flex vertical gap={16} style={{ width: 280 }}>
        {blocks}
      </Flex>
    </Flex>
  )
}
