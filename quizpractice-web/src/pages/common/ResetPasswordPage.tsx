import { useState } from 'react'
import { Alert, App, Button, Card, Flex, Form, Input, Result, Space, Spin, Typography } from 'antd'
import { useNavigate, useSearchParams } from 'react-router'
import type { NewPasswordInput } from '../../api/accountApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { useOpenAccountModal } from '../../components/account/accountModalContext'
import { NewPasswordFields } from '../../components/account/NewPasswordFields'
import { useResetLinkCheck, useResetPassword, useSendPasswordResetLink } from '../../hooks/useAccount'

/**
 * Reset Password (SRS III 1.3; F#3). Two views of one page: the request form, opened
 * from the login pop-up (UC-04), and the new password form, opened from the link in
 * the mailbox, /reset-password?token=… (UC-05).
 */
export function ResetPasswordPage() {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''

  return (
    <Flex justify="center" style={{ paddingTop: 48 }}>
      <Card title="Reset Password" style={{ width: 440 }}>
        {token ? <NewPasswordForm key={token} token={token} /> : <RequestLinkForm />}
      </Card>
    </Flex>
  )
}

/** UC-05: the link is checked first (step 2); the form only shows for a link that still works. */
function NewPasswordForm({ token }: { token: string }) {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const openModal = useOpenAccountModal()
  const [form] = Form.useForm<NewPasswordInput>()
  const check = useResetLinkCheck(token)
  const reset = useResetPassword(token)
  const [linkDead, setLinkDead] = useState(false)

  if (check.isPending) return <Spin description="Checking the link…" style={{ display: 'block', padding: 32 }} />

  // E1: expired, already used, or ended by another link that set a password (A1 of UC-04).
  if (check.isError || linkDead) {
    return (
      <Result
        status="error"
        title="This reset link no longer works"
        subTitle="It has expired or has already been used. Ask for a new one."
        extra={
          <Button type="primary" onClick={() => navigate('/reset-password')}>
            Ask for a new link
          </Button>
        }
      />
    )
  }

  const onFinish = (values: NewPasswordInput) =>
    reset.mutate(values, {
      onSuccess: () => {
        message.success('Your password has been changed. Log in with the new one.')
        navigate('/')
        openModal('login')
      },
      onError: (e) => {
        if (problemOf(e)?.code === 'Link.Invalid') setLinkDead(true)
        else if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
      },
    })

  return (
    <Form<NewPasswordInput> form={form} layout="vertical" onFinish={onFinish}>
      <NewPasswordFields />
      <Flex justify="end">
        <Space>
          <Button onClick={() => navigate('/')}>Cancel</Button>
          <Button type="primary" htmlType="submit" loading={reset.isPending}>
            Save Password
          </Button>
        </Space>
      </Flex>
    </Form>
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
