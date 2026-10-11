import { useEffect, useRef } from 'react'
import { App, Button, Form, Input, Result, Space, Spin } from 'antd'
import { useNavigate, useSearchParams } from 'react-router'
import { errorMessage } from '../../api/client'
import { useOpenAccountModal } from '../../components/account/accountModalContext'
import { useSendVerificationLink, useVerifyEmail } from '../../hooks/useAccount'

/**
 * Verify Email (SRS III 1.2, second part; F#2; UC-03): the page the link of the
 * verification mail opens, /verify-email?token=…. It posts the token once and shows
 * what happened, then offers the login pop-up.
 */
export function VerifyEmailPage() {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const verify = useVerifyEmail()
  const sent = useRef(false)

  useEffect(() => {
    // Once: the second call of React's development double run would only answer
    // "already verified" and hide the real result.
    if (token && !sent.current) {
      sent.current = true
      verify.mutate(token)
    }
  }, [token, verify])

  if (!token) return <InvalidLink />
  if (verify.isError) return <InvalidLink />
  if (!verify.data) return <Spin description="Verifying your email…" size="large" style={{ display: 'block', marginTop: 80 }} />

  const { email, alreadyVerified, passwordSent } = verify.data
  if (alreadyVerified) {
    return (
      <Result
        status="info"
        title="This account is already verified"
        subTitle={`${email} can sign in with the password sent by mail.`}
        extra={<LoginButton />}
      />
    )
  }

  if (!passwordSent) {
    // E3: verified, but the password mail failed; a reset link sets one instead.
    return (
      <Result
        status="warning"
        title="Your email is verified"
        subTitle="The mail with your login password could not be sent. Set a password with a reset link instead."
        extra={<ResetButton />}
      />
    )
  }

  return (
    <Result
      status="success"
      title="Your email is verified"
      subTitle={
        <>
          Your login password has been sent to <b>{email}</b>. Read it in your mailbox, then log in. You can change it
          later with Change Password.
        </>
      }
      extra={<LoginButton />}
    />
  )
}

function LoginButton() {
  const openModal = useOpenAccountModal()
  return (
    <Button type="primary" onClick={() => openModal('login')}>
      Login
    </Button>
  )
}

function ResetButton() {
  const navigate = useNavigate()
  return (
    <Button type="primary" onClick={() => navigate('/reset-password')}>
      Reset Password
    </Button>
  )
}

/** E1: the link is not genuine, was cut, or has expired; a new one can be sent. */
function InvalidLink() {
  const { message } = App.useApp()
  const resend = useSendVerificationLink()
  return (
    <Result
      status="error"
      title="This verification link is not valid or has expired"
      subTitle="Enter the email you registered with to get a new link."
      extra={
        <Form
          layout="inline"
          style={{ justifyContent: 'center' }}
          onFinish={({ email }: { email: string }) =>
            resend.mutate(email, {
              onSuccess: () => message.success('If this email is waiting for verification, a new link has been sent.'),
              onError: (e) => message.error(errorMessage(e)),
            })
          }
        >
          <Space.Compact>
            <Form.Item
              name="email"
              noStyle
              rules={[
                { required: true, message: 'Enter the email.' },
                { type: 'email', message: 'Enter a valid email address.' },
              ]}
            >
              <Input placeholder="Email" style={{ width: 260 }} />
            </Form.Item>
            <Button type="primary" htmlType="submit" loading={resend.isPending}>
              Send a new link
            </Button>
          </Space.Compact>
        </Form>
      }
    />
  )
}
