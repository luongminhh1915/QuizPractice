import { Avatar, Breadcrumb, Dropdown, Flex, Layout, Menu, Typography } from 'antd'
import {
  DashboardOutlined,
  FileTextOutlined,
  FormOutlined,
  LogoutOutlined,
  PictureOutlined,
  QuestionCircleOutlined,
  UserOutlined,
} from '@ant-design/icons'
import { Link, Outlet, useLocation } from 'react-router'

const { Header, Sider, Content } = Layout

// TODO(auth, BR-16, D23): build this from the System Menu settings the signed-in role
// may open. Until sign-in exists it is the Marketing menu, the role of the sample,
// followed by the Expert screens built so far.
const ROLE_MENU = [
  { key: '/manage/dashboard', icon: <DashboardOutlined />, label: 'Dashboard' },
  { key: '/manage/posts', icon: <FileTextOutlined />, label: 'Posts' },
  { key: '/manage/sliders', icon: <PictureOutlined />, label: 'Sliders' },
  { key: '/manage/questions', icon: <QuestionCircleOutlined />, label: 'Questions' },
  { key: '/manage/quizzes', icon: <FormOutlined />, label: 'Quizzes' },
]

// Breadcrumb labels by path segment; a number is a record id.
const CRUMBS: Record<string, string> = {
  manage: 'Management',
  posts: 'Posts',
  sliders: 'Sliders',
  questions: 'Questions',
  quizzes: 'Quizzes',
  new: 'New',
}

/**
 * Frame of every Marketing, Sale, Expert and Admin screen (SRS III 0.1): system name,
 * role menu, account menu, breadcrumb; the screen fills the content area.
 */
export function ManagementLayout() {
  const { pathname } = useLocation()
  const selected = ROLE_MENU.find((m) => pathname.startsWith(m.key))?.key

  const segments = pathname.split('/').filter(Boolean)
  const crumbs = segments.map((s, i) => {
    const label = CRUMBS[s] ?? (/^\d+$/.test(s) ? `#${s}` : s)
    const to = '/' + segments.slice(0, i + 1).join('/')
    return { title: i < segments.length - 1 && i > 0 ? <Link to={to}>{label}</Link> : label }
  })

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
          selectedKeys={selected ? [selected] : []}
          items={ROLE_MENU.map((m) => ({ key: m.key, icon: m.icon, label: <Link to={m.key}>{m.label}</Link> }))}
          style={{ borderInlineEnd: 0 }}
        />
      </Sider>
      <Layout>
        <Header style={{ background: '#fff', padding: '0 24px', borderBottom: '1px solid #f0f0f0' }}>
          <Flex justify="space-between" align="center" style={{ height: '100%' }}>
            <Breadcrumb items={crumbs} />
            {/* TODO(auth): email and role of the signed-in user (UC-06, UC-07, UC-08). */}
            <Dropdown
              menu={{
                items: [
                  { key: 'profile', icon: <UserOutlined />, label: 'User Profile' },
                  { key: 'password', label: 'Change Password' },
                  { type: 'divider' },
                  { key: 'logout', icon: <LogoutOutlined />, label: 'Logout' },
                ],
              }}
            >
              <Flex gap={8} align="center" style={{ cursor: 'pointer' }}>
                <Avatar size="small" icon={<UserOutlined />} />
                <span>marketing@quizpractice.vn · Marketing</span>
              </Flex>
            </Dropdown>
          </Flex>
        </Header>
        <Content style={{ margin: 24 }}>
          <div style={{ background: '#fff', padding: 24, borderRadius: 8 }}>
            <Outlet />
          </div>
        </Content>
      </Layout>
    </Layout>
  )
}
