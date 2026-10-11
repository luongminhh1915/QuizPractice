import { Form, Input } from 'antd'
import { PASSWORD_MAX, PASSWORD_MIN, PASSWORD_PATTERN } from '../../constants/fieldLength'

/**
 * New Password and Confirm New Password (SRS III 1.3, 1.5), with the password rule of
 * BR-17 checked before sending. The API checks it again.
 */
export function NewPasswordFields() {
  return (
    <>
      <Form.Item
        label="New Password"
        name="newPassword"
        rules={[
          { required: true, message: 'Enter the new password.' },
          { min: PASSWORD_MIN, max: PASSWORD_MAX, message: `The password must have ${PASSWORD_MIN} to ${PASSWORD_MAX} characters.` },
          { pattern: PASSWORD_PATTERN, message: 'The password needs at least one letter and one digit.' },
        ]}
        extra={`${PASSWORD_MIN} to ${PASSWORD_MAX} characters, with at least one letter and one digit.`}
      >
        <Input.Password autoComplete="new-password" />
      </Form.Item>
      <Form.Item
        label="Confirm New Password"
        name="confirmPassword"
        dependencies={['newPassword']}
        rules={[
          { required: true, message: 'Enter the new password again.' },
          ({ getFieldValue }) => ({
            validator: (_, value) =>
              !value || value === getFieldValue('newPassword')
                ? Promise.resolve()
                : Promise.reject(new Error('The two passwords do not match.')),
          }),
        ]}
      >
        <Input.Password autoComplete="new-password" />
      </Form.Item>
    </>
  )
}
