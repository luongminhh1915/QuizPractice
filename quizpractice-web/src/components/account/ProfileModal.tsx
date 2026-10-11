import { useEffect, useRef, useState } from 'react'
import { App, Avatar, Button, Flex, Form, Input, Modal, Radio, Spin, Upload } from 'antd'
import { UserOutlined } from '@ant-design/icons'
import type { ProfileInput } from '../../api/accountApi'
import { applyFieldErrors, errorMessage } from '../../api/client'
import { FULL_NAME_MAX, FULL_NAME_MIN, IMAGE_MAX_BYTES, IMAGE_TYPES, MOBILE_PATTERN } from '../../constants/fieldLength'
import { useProfile, useSaveProfile } from '../../hooks/useAccount'

type FormValues = Omit<ProfileInput, 'avatar' | 'removeAvatar'> & { email: string }

/**
 * User Profile (SRS III 1.4; F#5; UC-06). The email is the sign-in name and stays
 * read-only (BR-04). A new avatar is only sent with Save.
 */
export function ProfileModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { message } = App.useApp()
  const [form] = Form.useForm<FormValues>()
  const profile = useProfile(open)
  const save = useSaveProfile()
  const [avatarFile, setAvatarFile] = useState<File>()
  const [removeAvatar, setRemoveAvatar] = useState(false)
  const [preview, setPreview] = useState<string>()
  const previewUrl = useRef<string>(undefined)

  const setFile = (file: File | undefined) => {
    if (previewUrl.current) URL.revokeObjectURL(previewUrl.current)
    previewUrl.current = file ? URL.createObjectURL(file) : undefined
    setPreview(previewUrl.current)
    setAvatarFile(file)
  }

  useEffect(() => {
    if (open && profile.data) {
      const { email, fullName, gender, mobile } = profile.data
      form.setFieldsValue({ email, fullName, gender, mobile })
    }
  }, [open, profile.data, form])

  // A2 and A1: closing keeps nothing that was not saved.
  const close = () => {
    setFile(undefined)
    setRemoveAvatar(false)
    onClose()
  }

  const onFinish = ({ fullName, gender, mobile }: FormValues) =>
    save.mutate(
      { fullName, gender, mobile, avatar: avatarFile, removeAvatar },
      {
        onSuccess: () => {
          message.success('Your profile has been saved.')
          close()
        },
        // E1, E2: the pop-up stays open with what was entered.
        onError: (e) => {
          if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
        },
      },
    )

  const shown = preview ?? (removeAvatar ? undefined : (profile.data?.avatar ?? undefined))

  return (
    <Modal title="User Profile" open={open} onCancel={close} footer={null} destroyOnHidden width={480}>
      {profile.isPending ? (
        <Spin style={{ display: 'block', padding: 32 }} />
      ) : (
        <Form<FormValues> form={form} layout="vertical" onFinish={onFinish}>
          <Form.Item label="Avatar" extra="JPG or PNG, up to 2 MB.">
            <Flex gap={16} align="center">
              <Avatar size={72} src={shown} icon={<UserOutlined />} />
              <Upload
                accept={IMAGE_TYPES.join(',')}
                showUploadList={false}
                beforeUpload={(file) => {
                  if (!IMAGE_TYPES.includes(file.type)) message.error('The avatar must be a JPG or PNG file.')
                  else if (file.size > IMAGE_MAX_BYTES) message.error('The avatar must be 2 MB or smaller.')
                  else {
                    setFile(file)
                    setRemoveAvatar(false)
                  }
                  return false
                }}
              >
                <Button>Change</Button>
              </Upload>
              {shown && (
                <Button
                  onClick={() => {
                    setFile(undefined)
                    setRemoveAvatar(true)
                  }}
                >
                  Remove
                </Button>
              )}
            </Flex>
          </Form.Item>
          <Form.Item
            label="Full Name"
            name="fullName"
            rules={[
              { required: true, whitespace: true, message: 'Enter the full name.' },
              { min: FULL_NAME_MIN, max: FULL_NAME_MAX, message: `The full name must have ${FULL_NAME_MIN} to ${FULL_NAME_MAX} characters.` },
            ]}
          >
            <Input autoComplete="name" />
          </Form.Item>
          <Form.Item label="Gender" name="gender" rules={[{ required: true, message: 'Choose the gender.' }]}>
            <Radio.Group options={['Male', 'Female', 'Other']} />
          </Form.Item>
          <Form.Item label="Email" name="email" extra="The email is your sign-in name and cannot be changed.">
            <Input disabled />
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
          <Flex justify="end" gap={8}>
            <Button onClick={close}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={save.isPending}>
              Save
            </Button>
          </Flex>
        </Form>
      )}
    </Modal>
  )
}
