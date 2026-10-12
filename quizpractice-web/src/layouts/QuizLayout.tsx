import { Layout, Typography } from 'antd'
import { Outlet } from 'react-router'
import { useCurrentUser } from '../hooks/useAccount'

/**
 * Frame of Quiz Handle and Quiz Review (SRS III 4.1, 4.2): no site menu and no link out,
 * so a stray click cannot leave a running attempt. The pages offer their own way back.
 */
export function QuizLayout() {
  // Loads who is signed in and the antiforgery token every change needs.
  useCurrentUser()

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Layout.Header style={{ background: '#fff', borderBottom: '1px solid #f0f0f0', padding: '0 32px' }}>
        <Typography.Title level={4} style={{ margin: 0, lineHeight: '64px' }}>
          QuizPractice
        </Typography.Title>
      </Layout.Header>
      <Layout.Content style={{ padding: '24px 32px', maxWidth: 960, width: '100%', margin: '0 auto' }}>
        <Outlet />
      </Layout.Content>
    </Layout>
  )
}
