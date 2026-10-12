import { useEffect } from 'react'
import { Alert, App, Button, Form, Input, InputNumber, Radio, Result, Select, Space, Spin, Tag } from 'antd'
import { useLocation, useNavigate, useParams, useSearchParams } from 'react-router'
import { SETTING_TYPE_LABELS, type SettingInput, type SettingSaved, type SettingType } from '../../api/settingsApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { BRIEF_MAX, NAME_MAX } from '../../constants/fieldLength'
import { useSaveSetting, useSetting } from '../../hooks/useSettings'

/** Types the system reads by value: no new value, and a stored one keeps its value (Duck, 12/10). */
const LOCKED: SettingType[] = ['UserRole', 'SystemMenu', 'LessonType', 'TestType']
const OPEN_TYPES = (Object.entries(SETTING_TYPE_LABELS) as [SettingType, string][]).filter(([t]) => !LOCKED.includes(t))

/**
 * Setting Details (SRS III 9.4; PDF II.9, F#40; UC-65 Add or Edit Setting). One form for add
 * and edit; a stored setting keeps its type (D24). ?view opens it read only.
 */
export function SettingDetailsPage() {
  const { message, modal } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const params = useParams()
  const [search] = useSearchParams()
  const id = params.id ? Number(params.id) : undefined
  const isNew = id === undefined
  const readOnly = search.has('view')

  const [form] = Form.useForm<SettingInput>()
  const stored = useSetting(id)
  const save = useSaveSetting(id)

  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/settings'
  const s = stored.data

  useEffect(() => {
    if (s) {
      const { type, value, displayOrder, description, status } = s
      form.setFieldsValue({ type, value, displayOrder, description: description ?? '', status })
    }
  }, [s, form])

  if (!isNew && stored.isPending) return <Spin />
  if (!isNew && stored.isError) {
    const notFound = problemOf(stored.error)?.status === 404
    return (
      <Result
        status={notFound ? '404' : 'error'}
        title={notFound ? 'This setting does not exist.' : errorMessage(stored.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Settings List</Button>}
      />
    )
  }

  const done = (saved: SettingSaved) => {
    message.success(`"${saved.setting.value}" was saved.`)
    if (saved.quizzesFollowed > 0) message.info(`${saved.quizzesFollowed} quiz(zes) drawing by this type now use the new name.`, 8)
    navigate(backTo)
  }

  const onFinish = (values: SettingInput, confirmInUse = false) =>
    save.mutate(
      { ...values, version: s?.version, confirmInUse },
      {
        onSuccess: done,
        onError: (e) => {
          const code = problemOf(e)?.code
          // E2: another Admin saved meanwhile; show the current values and ask for the change again.
          if (code === 'Setting.Changed') {
            message.warning(errorMessage(e), 8)
            void stored.refetch()
            return
          }
          // UC-66 E1: records still use the value; switch it off only once confirmed.
          if (code === 'Setting.InUse') {
            modal.confirm({
              title: 'Switch off a value in use?',
              content: errorMessage(e),
              okText: 'Switch off',
              cancelText: 'Cancel',
              onOk: () => onFinish(values, true),
            })
            return
          }
          if (!applyFieldErrors(form, e)) message.error(errorMessage(e), 8)
        },
      },
    )

  const valueLocked = !!s?.valueLocked

  return (
    <>
      <PageHeader title={isNew ? 'New Setting' : readOnly ? 'Setting Details' : 'Edit Setting'} />
      <Form<SettingInput>
        form={form}
        name="setting"
        layout="vertical"
        disabled={readOnly}
        initialValues={{ status: 'Active', description: '' }}
        onFinish={(values) => onFinish(values)}
        style={{ maxWidth: 640 }}
      >
        <Form.Item label="Type" name="type" rules={[{ required: true, message: 'Choose the type.' }]}>
          {isNew ? (
            <Select
              options={OPEN_TYPES.map(([value, label]) => ({ value, label }))}
              placeholder="Choose a type"
              style={{ width: 260 }}
            />
          ) : (
            // Step 3: a stored setting keeps its type.
            <Tag>{s && SETTING_TYPE_LABELS[s.type]}</Tag>
          )}
        </Form.Item>

        <Form.Item
          label="Value"
          name="value"
          rules={[{ required: true, whitespace: true, message: 'Enter the value.' }, { max: NAME_MAX }]}
          extra={valueLocked ? 'The system reads this value, so it cannot change.' : 'Unique within its type.'}
        >
          <Input disabled={readOnly || valueLocked} />
        </Form.Item>

        <Space size="large" align="start" wrap>
          <Form.Item label="Order" name="displayOrder" extra={isNew ? 'Empty: after the last value of the type.' : undefined}>
            <InputNumber min={1} precision={0} style={{ width: 120 }} />
          </Form.Item>
          <Form.Item
            label="Status"
            name="status"
            extra={s && !s.canDeactivate ? 'Needed by the system: it stays Active.' : 'Only active values are offered by the screens.'}
          >
            <Radio.Group optionType="button" options={['Active', 'Inactive']} disabled={readOnly || (s && !s.canDeactivate)} />
          </Form.Item>
        </Space>

        <Form.Item label="Description" name="description" rules={[{ max: BRIEF_MAX }]}>
          <Input.TextArea rows={3} />
        </Form.Item>

        {s?.type === 'SystemMenu' && (
          // A4: the roles come from the permission table of the configuration file (F#4, BR-03).
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 24 }}
            title={`Menu Access: ${s.menuRoles.join(', ') || 'no role'}`}
            description="The roles that see this item, and may open its pages, follow the permission table of the configuration file."
          />
        )}

        <Space style={{ display: 'flex' }}>
          {!readOnly && (
            <Button type="primary" htmlType="submit" loading={save.isPending}>
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
