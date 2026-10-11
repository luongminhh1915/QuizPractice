import { useState } from 'react'
import { Alert, App, Button, Flex, Form, Input, Modal, Radio, Result, Space, Typography } from 'antd'
import { Link } from 'react-router'
import type { RegisterInput } from '../../api/accountApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { FULL_NAME_MAX, FULL_NAME_MIN, MOBILE_PATTERN } from '../../constants/fieldLength'
import { useRegister, useSendVerificationLink } from '../../hooks/useAccount'
import { useOpenAccountModal } from './accountModalContext'

type Outcome =
  | { kind: 'sent'; email: string }
  | { kind: 'taken' }
  | { kind: 'mailFailed'; email: string; text: string }
  | { kind: 'error'; text: string }

/**
 * User Register (SRS III 1.2; F#2; UC-02). No password field: the login password is
 * mailed once the email is verified (OI-16).
 */
export function RegisterModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { message } = App.useApp()
  const openModal = useOpenAccountModal()
  const [form] = Form.useForm<RegisterInput>()
  const register = useRegister()
  const resend = useSendVerificationLink()
  const [outcome, setOutcome] = useState<Outcome>()

  const close = () => {
    setOutcome(undefined)
    onClose()
  }

  const onFinish = (values: RegisterInput) => {
    setOutcome(undefined)
    register.mutate(values, {
      onSuccess: (r) => setOutcome({ kind: 'sent', email: r.email }),
      onError: (e) => {
        if (applyFieldErrors(form, e)) return
        const code = problemOf(e)?.code
        if (code === 'Account.EmailTaken') setOutcome({ kind: 'taken' })
        else if (code === 'Mail.NotSent') setOutcome({ kind: 'mailFailed', email: values.email, text: errorMessage(e) })
        else setOutcome({ kind: 'error', text: errorMessage(e) })
      },
    })
  }

  const sendLink = (email: string) =>
    resend.mutate(email, {
      onSuccess: () => setOutcome({ kind: 'sent', email }),
      onError: (e) => message.error(errorMessage(e)),
    })

  // Step 7: what to do next, in place of the form.
  if (outcome?.kind === 'sent') {
    return (
      <Modal title="Register" open={open} onCancel={close} footer={null} destroyOnHidden width={480}>
        <Result
          status="success"
          title="Check your mailbox"
          subTitle={
            <>
              A verification link has been sent to <b>{outcome.email}</b>. Open it to verify the account; your login
              password will then be sent to the same address.
            </>
          }
          extra={<Button onClick={close}>Close</Button>}
        />
      </Modal>
    )
  }

  return (
    <Modal title="Register" open={open} onCancel={close} footer={null} destroyOnHidden width={480}>
      <Form<RegisterInput> form={form} layout="vertical" onFinish={onFinish}>
        {outcome?.kind === 'taken' && (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 16 }}
            title="This email is already registered."
            description={
              <Space size={16}>
                <Typography.Link onClick={() => openModal('login')}>Log in</Typography.Link>
                <Link to="/reset-password" onClick={close}>
                  Reset the password
                </Link>
              </Space>
            }
          />
        )}
        {outcome?.kind === 'mailFailed' && (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 16 }}
            title={outcome.text}
            action={
              <Button size="small" loading={resend.isPending} onClick={() => sendLink(outcome.email)}>
                Send the link again
              </Button>
            }
          />
        )}
        {outcome?.kind === 'error' && <Alert type="error" showIcon style={{ marginBottom: 16 }} title={outcome.text} />}

        <Form.Item
          label="Full Name"
          name="fullName"
          rules={[
            { required: true, whitespace: true, message: 'Enter the full name.' },
            { min: FULL_NAME_MIN, max: FULL_NAME_MAX, message: `The full name must have ${FULL_NAME_MIN} to ${FULL_NAME_MAX} characters.` },
          ]}
        >
          <Input autoFocus autoComplete="name" />
        </Form.Item>
        <Form.Item label="Gender" name="gender" rules={[{ required: true, message: 'Choose the gender.' }]}>
          <Radio.Group options={['Male', 'Female', 'Other']} />
        </Form.Item>
        <Form.Item
          label="Email"
          name="email"
          rules={[
            { required: true, message: 'Enter the email.' },
            { type: 'email', message: 'Enter a valid email address.' },
          ]}
        >
          <Input autoComplete="email" />
        </Form.Item>
        <Form.Item
          label="Mobile"
          name="mobile"
          rules={[
            { required: true, message: 'Enter the mobile number.' },
            { pattern: MOBILE_PATTERN, message: 'The mobile number must have 10 digits and start with 0.' },
          ]}
        >
          <Input autoComplete="tel" maxLength={10} />
        </Form.Item>
        <Flex justify="space-between" align="center">
          <Typography.Text type="secondary">
            Have an account? <Typography.Link onClick={() => openModal('login')}>Login</Typography.Link>
          </Typography.Text>
          <Space>
            <Button onClick={close}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={register.isPending}>
              Register
            </Button>
          </Space>
        </Flex>
      </Form>
    </Modal>
  )
}
