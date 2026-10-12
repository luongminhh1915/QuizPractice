import { useEffect } from 'react'
import { Alert, App, Button, Form, Input, Radio, Result, Select, Space, Spin, Typography } from 'antd'
import { useLocation, useNavigate, useParams, useSearchParams } from 'react-router'
import type { NewUserInput, UserEditInput, UserSaved } from '../../api/usersApi'
import { usersApi } from '../../api/usersApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { ImageUpload } from '../../components/ImageUpload'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { ADDRESS_MAX, FULL_NAME_MAX, FULL_NAME_MIN, MOBILE_PATTERN } from '../../constants/fieldLength'
import { useCurrentUser } from '../../hooks/useAccount'
import { useCreateUser, useUpdateUser, useUser, useUserChoices } from '../../hooks/useUsers'

type FormValues = NewUserInput & UserEditInput

/**
 * User Details (SRS III 9.2; PDF II.9, F#38; UC-63 Add or Edit User). A new account takes
 * every field; a stored one only its role and status (BR-20). ?view opens it read only.
 */
export function UserDetailsPage() {
  const { message, modal } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const params = useParams()
  const [search] = useSearchParams()
  const id = params.id ? Number(params.id) : undefined
  const isNew = id === undefined
  const readOnly = search.has('view')

  const [form] = Form.useForm<FormValues>()
  const stored = useUser(id)
  const choices = useUserChoices()
  const create = useCreateUser()
  const update = useUpdateUser(id)
  const me = useCurrentUser().data

  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/users'
  const isSelf = stored.data?.id === me?.id

  useEffect(() => {
    if (stored.data) {
      const { roleId, status } = stored.data
      form.setFieldsValue({ roleId, status })
    }
  }, [stored.data, form])

  if (!isNew && stored.isPending) return <Spin />
  if (!isNew && stored.isError) {
    const notFound = problemOf(stored.error)?.status === 404
    return (
      <Result
        status={notFound ? '404' : 'error'}
        title={notFound ? 'This account does not exist.' : errorMessage(stored.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Users List</Button>}
      />
    )
  }

  /** After a save: E2 (password not emailed) and A3 (subjects released) are said before leaving. */
  const done = (saved: UserSaved, verb: string) => {
    if (saved.passwordSent === false) {
      message.warning(
        `${saved.user.fullName}'s account was ${verb}, but the password email could not be sent. ` +
          'They can use Forgot Password on the sign-in pop-up to get a link.',
        10,
      )
    } else {
      message.success(`${saved.user.fullName}'s account was ${verb}.${saved.passwordSent ? ' The password was emailed.' : ''}`)
    }
    if (saved.releasedSubjects.length > 0) {
      message.info(`No owner now: ${saved.releasedSubjects.join(', ')}. Assign one on each subject's Overview tab.`, 10)
    }
    navigate(backTo)
  }

  const onCreate = (values: FormValues) =>
    create.mutate(values, {
      onSuccess: (saved) => done(saved, 'created'),
      onError: async (e) => {
        // E1: the email has an account already; open that one instead.
        if (problemOf(e)?.code === 'User.EmailTaken') {
          const found = await usersApi.list({ page: 1, pageSize: 10, search: values.email.trim() })
          const existing = found.items.find((u) => u.email.toLowerCase() === values.email.trim().toLowerCase())
          message.info(errorMessage(e), 6)
          if (existing) navigate(`/manage/users/${existing.id}`, { replace: true, state: location.state })
          return
        }
        if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
      },
    })

  const onUpdate = (values: FormValues, confirmHandOver = false) =>
    update.mutate(
      { roleId: values.roleId, status: values.status, confirmHandOver },
      {
        onSuccess: (saved) => done(saved, 'saved'),
        onError: (e) => {
          // A3: removing the Expert role releases the subjects the user owns, once confirmed.
          if (problemOf(e)?.code === 'User.OwnsSubjects') {
            modal.confirm({
              title: 'Remove the Expert role?',
              content: errorMessage(e),
              okText: 'Remove the role',
              cancelText: 'Cancel',
              onOk: () => onUpdate(values, true),
            })
            return
          }
          if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
        },
      },
    )

  const roles = (choices.data?.roles ?? []).map((r) => ({ value: r.id, label: r.name }))
  // A stored account keeps a role switched off since (BR-16).
  if (stored.data && !roles.some((r) => r.value === stored.data!.roleId)) {
    roles.push({ value: stored.data.roleId, label: `${stored.data.role} (no longer in use)` })
  }

  const u = stored.data
  const title = isNew ? 'New User' : readOnly ? 'User Details' : 'Edit User'

  return (
    <>
      <PageHeader title={title} />
      <Form<FormValues>
        form={form}
        name="user"
        layout="vertical"
        disabled={readOnly}
        initialValues={{ gender: 'Male' }}
        onFinish={(values) => (isNew ? onCreate(values) : onUpdate(values))}
        style={{ maxWidth: 720 }}
      >
        {isNew ? (
          <>
            <Form.Item label="Avatar" name="avatar" extra="JPG or PNG, up to 2 MB. Optional.">
              <ImageUpload width={160} />
            </Form.Item>
            <Form.Item
              label="Full Name"
              name="fullName"
              rules={[{ required: true, whitespace: true, message: 'Enter the full name.' }, { min: FULL_NAME_MIN }, { max: FULL_NAME_MAX }]}
            >
              <Input />
            </Form.Item>
            <Form.Item label="Gender" name="gender" rules={[{ required: true }]}>
              <Radio.Group options={['Male', 'Female', 'Other']} />
            </Form.Item>
            <Space size="large" align="start" wrap>
              <Form.Item label="Email" name="email" rules={[{ required: true, message: 'Enter the email.' }, { type: 'email', message: 'Enter a valid email.' }]}>
                <Input style={{ width: 320 }} />
              </Form.Item>
              <Form.Item
                label="Mobile"
                name="mobile"
                rules={[{ required: true, message: 'Enter the mobile number.' }, { pattern: MOBILE_PATTERN, message: '10 digits starting with 0.' }]}
              >
                <Input style={{ width: 200 }} />
              </Form.Item>
            </Space>
            <Form.Item label="Address" name="address" rules={[{ max: ADDRESS_MAX }]}>
              <Input />
            </Form.Item>
          </>
        ) : (
          u && (
            // BR-20: the rest of the account stays as the user entered it.
            <div style={{ display: 'flex', flexDirection: 'column', gap: 4, marginBottom: 24 }}>
              {u.avatar && <img src={u.avatar} alt="" width={96} height={96} style={{ objectFit: 'cover', borderRadius: 8 }} />}
              <Typography.Title level={4} style={{ margin: 0 }}>
                {u.fullName}
              </Typography.Title>
              <Typography.Text>
                {u.email} · {u.mobile} · {u.gender}
              </Typography.Text>
              {u.address && <Typography.Text type="secondary">{u.address}</Typography.Text>}
            </div>
          )
        )}

        <Space size="large" align="start" wrap>
          <Form.Item label="Role" name="roleId" rules={[{ required: true, message: 'Choose the role.' }]}>
            <Select options={roles} loading={choices.isPending} disabled={readOnly || isSelf} style={{ width: 200 }} />
          </Form.Item>
          {isNew ? (
            <Form.Item label="Status" extra="A new account starts Active.">
              <StatusTag status="Active" />
            </Form.Item>
          ) : (
            <Form.Item
              label="Status"
              name="status"
              extra={u?.status === 'Unverified' ? 'Setting it Active counts as verified and emails a password (BR-01).' : undefined}
            >
              <Radio.Group
                disabled={readOnly || isSelf}
                optionType="button"
                options={[
                  ...(u?.status === 'Unverified' ? [{ value: 'Unverified', label: 'Unverified', disabled: true }] : []),
                  { value: 'Active', label: 'Active' },
                  { value: 'Inactive', label: 'Inactive' },
                ]}
              />
            </Form.Item>
          )}
        </Space>

        {isSelf && !readOnly && (
          // E3: the Admin cannot lock itself out.
          <Alert type="info" showIcon style={{ marginBottom: 24 }} title="This is your own account: its role and status cannot be changed here." />
        )}
        {isNew && (
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 24 }}
            title="A password is generated and emailed with the login link when the account is saved."
          />
        )}

        <Space style={{ display: 'flex' }}>
          {!readOnly && (
            <Button type="primary" htmlType="submit" loading={create.isPending || update.isPending} disabled={isSelf}>
              Save
            </Button>
          )}
          <Button disabled={false} onClick={() => navigate(backTo)}>
            {readOnly ? 'Back' : 'Cancel'}
          </Button>
        </Space>
      </Form>
    </>
  )
}
