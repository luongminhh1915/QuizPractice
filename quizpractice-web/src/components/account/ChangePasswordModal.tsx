import { App, Button, Flex, Form, Input, Modal } from 'antd'
import type { ChangePasswordInput } from '../../api/accountApi'
import { applyFieldErrors, errorMessage } from '../../api/client'
import { useChangePassword } from '../../hooks/useAccount'
import { NewPasswordFields } from './NewPasswordFields'

/** Change Password (SRS III 1.5; F#6; UC-07). The session stays open after the change. */
export function ChangePasswordModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { message } = App.useApp()
  const [form] = Form.useForm<ChangePasswordInput>()
  const change = useChangePassword()

  const onFinish = (values: ChangePasswordInput) =>
    change.mutate(values, {
      onSuccess: () => {
        message.success('Your password has been changed.')
        onClose()
      },
      // E1 (wrong current password) and E2 come back as field errors; the pop-up stays open.
      onError: (e) => {
        if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
      },
    })

  return (
    <Modal title="Change Password" open={open} onCancel={onClose} footer={null} destroyOnHidden width={440}>
      <Form<ChangePasswordInput> form={form} layout="vertical" onFinish={onFinish}>
        <Form.Item
          label="Current Password"
          name="currentPassword"
          rules={[{ required: true, message: 'Enter the current password.' }]}
        >
          <Input.Password autoComplete="current-password" autoFocus />
        </Form.Item>
        <NewPasswordFields />
        <Flex justify="end" gap={8}>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="primary" htmlType="submit" loading={change.isPending}>
            Save
          </Button>
        </Flex>
      </Form>
    </Modal>
  )
}
