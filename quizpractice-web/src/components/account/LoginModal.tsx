import { useState } from 'react'
import { Alert, App, Button, Flex, Form, Input, Modal } from 'antd'
import { Link, useNavigate } from 'react-router'
import type { LoginInput } from '../../api/accountApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { firstScreen } from '../../constants/menu'
import { useLogin, useSendVerificationLink } from '../../hooks/useAccount'

/**
 * User Login (SRS III 1.1; F#1; UC-01). A management role lands on the first screen
 * of its menu; a customer stays on the page it signed in from.
 */
export function LoginModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const [form] = Form.useForm<LoginInput>()
  const login = useLogin()
  const resend = useSendVerificationLink()
  // E1 and E3 are shown in the pop-up, which stays open; E2 adds the resend button.
  const [refusal, setRefusal] = useState<{ text: string; unverifiedEmail?: string }>()

  const close = () => {
    setRefusal(undefined)
    onClose()
  }

  const onFinish = (values: LoginInput) => {
    setRefusal(undefined)
    login.mutate(values, {
      onSuccess: (user) => {
        message.success(`Welcome, ${user.fullName}.`)
        close()
        const home = firstScreen(user.menu)
        if (home) navigate(home)
      },
      onError: (e) => {
        if (applyFieldErrors(form, e)) return
        const unverified = problemOf(e)?.code === 'Auth.Unverified'
        setRefusal({ text: errorMessage(e), unverifiedEmail: unverified ? values.email : undefined })
      },
    })
  }

  const sendLink = (email: string) =>
    resend.mutate(email, {
      onSuccess: () => {
        message.success('A new verification link has been sent. Check your mailbox.')
        setRefusal(undefined)
      },
      onError: (e) => message.error(errorMessage(e)),
    })

  return (
    <Modal title="Login" open={open} onCancel={close} footer={null} destroyOnHidden width={420}>
      <Form<LoginInput> form={form} layout="vertical" onFinish={onFinish} requiredMark={false}>
        {refusal && (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 16 }}
            title={refusal.text}
            action={
              refusal.unverifiedEmail && (
                <Button size="small" loading={resend.isPending} onClick={() => sendLink(refusal.unverifiedEmail!)}>
                  Send the link again
                </Button>
              )
            }
          />
        )}
        <Form.Item label="Email" name="email" rules={[{ required: true, message: 'Enter the email.' }]}>
          <Input autoComplete="username" autoFocus />
        </Form.Item>
        <Form.Item label="Password" name="password" rules={[{ required: true, message: 'Enter the password.' }]}>
          <Input.Password autoComplete="current-password" />
        </Form.Item>
        <Flex justify="end" style={{ marginTop: -12, marginBottom: 16 }}>
          <Link to="/reset-password" onClick={close}>
            Forgot Password
          </Link>
        </Flex>
        <Button type="primary" htmlType="submit" block loading={login.isPending}>
          Login
        </Button>
      </Form>
    </Modal>
  )
}
