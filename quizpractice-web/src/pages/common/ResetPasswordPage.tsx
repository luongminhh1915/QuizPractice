import { useState } from 'react'
import { Alert, Button, Card, Flex, Form, Input, Result, Typography } from 'antd'
import { errorMessage } from '../../api/client'
import { useOpenAccountModal } from '../../components/account/accountModalContext'
import { useSendPasswordResetLink } from '../../hooks/useAccount'

/**
 * Reset Password (SRS III 1.3; F#3). The request form, opened from the login pop-up
 * (UC-04).
 */
export function ResetPasswordPage() {
  return (
    <Flex justify="center" style={{ paddingTop: 48 }}>
      <Card title="Reset Password" style={{ width: 440 }}>
        <RequestLinkForm />
      </Card>
    </Flex>
  )
}

/** UC-04: the same confirmation whether or not the email is registered (step 6). */
function RequestLinkForm() {
  const openModal = useOpenAccountModal()
  const send = useSendPasswordResetLink()
  const [sentTo, setSentTo] = useState<string>()

  if (sentTo) {
    return (
      <Result
        status="success"
        title="Check your mailbox"
        subTitle={`If ${sentTo} belongs to an account, a reset link has been sent to it. The link works for a limited time.`}
        extra={[
          <Button key="again" onClick={() => setSentTo(undefined)}>
            Send another link
          </Button>,
          <Button key="login" type="primary" onClick={() => openModal('login')}>
            Back to Login
          </Button>,
        ]}
      />
    )
  }

  return (
    <Form layout="vertical" onFinish={({ email }: { email: string }) => send.mutate(email, { onSuccess: () => setSentTo(email) })}>
      <Typography.Paragraph type="secondary">Enter the email of your account. We will send you a link to set a new password.</Typography.Paragraph>
      {/* E1: the mail could not be sent; nothing says whether the email is registered. */}
      {send.isError && <Alert type="error" showIcon style={{ marginBottom: 16 }} title={errorMessage(send.error)} />}
      <Form.Item
        label="Email"
        name="email"
        rules={[
          { required: true, message: 'Enter the email.' },
          { type: 'email', message: 'Enter a valid email address.' },
        ]}
      >
        <Input autoFocus autoComplete="email" />
      </Form.Item>
      <Button type="primary" htmlType="submit" block loading={send.isPending}>
        Send Reset Link
      </Button>
      <Flex justify="center" style={{ marginTop: 16 }}>
        <Typography.Link onClick={() => openModal('login')}>Back to Login</Typography.Link>
      </Flex>
    </Form>
  )
}
