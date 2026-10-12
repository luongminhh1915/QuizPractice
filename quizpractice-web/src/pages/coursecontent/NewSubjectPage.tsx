import { App, Button, Form, Input, InputNumber, Select, Space, Switch, Tag } from 'antd'
import { useLocation, useNavigate } from 'react-router'
import type { NewSubjectInput } from '../../api/subjectsApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { ImageUpload } from '../../components/ImageUpload'
import { PageHeader } from '../../components/PageHeader'
import { RichTextEditor } from '../../components/RichTextEditor'
import { NAME_MAX } from '../../constants/fieldLength'
import { useCreateSubject, useSubjectChoices } from '../../hooks/useSubjects'

/** Pass mark a new subject starts from (Subject.DefaultPassRate, BR-21). */
const DEFAULT_PASS_RATE = 50

/**
 * New Subject (SRS III 6.2; PDF II.6, F#24; UC-42 Create Subject). Admin only. Save opens
 * Subject Management Details of the new subject so the content preparation can start (step 5).
 */
export function NewSubjectPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const [form] = Form.useForm<NewSubjectInput>()
  const choices = useSubjectChoices()
  const create = useCreateSubject()

  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/subjects'

  const onFinish = (values: NewSubjectInput) =>
    create.mutate(values, {
      onSuccess: (subject) => {
        message.success(`Subject "${subject.name}" was created.`)
        navigate(`/manage/subjects/${subject.id}`, { replace: true, state: { back: backTo } })
      },
      onError: (e) => {
        // E2: the owner lost the Expert role meanwhile; reload the list to choose again.
        if (problemOf(e)?.errors?.ExpertId) void choices.refetch()
        // E1: keep everything entered and mark the fields to correct.
        if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
      },
    })

  return (
    <>
      <PageHeader title="New Subject" />
      <Form<NewSubjectInput>
        form={form}
        layout="vertical"
        initialValues={{ isFeatured: false, passRate: DEFAULT_PASS_RATE, description: '' }}
        onFinish={onFinish}
        style={{ maxWidth: 880 }}
      >
        <Form.Item
          label="Thumbnail"
          name="thumbnail"
          rules={[{ required: true, message: 'A thumbnail is required for a new subject.' }]}
          extra="JPG or PNG, up to 2 MB. Shown on the subject cards of the public pages."
        >
          <ImageUpload width={280} />
        </Form.Item>

        <Form.Item label="Name" name="name" rules={[{ required: true, whitespace: true, message: 'Enter the subject name.' }, { max: NAME_MAX }]}>
          <Input />
        </Form.Item>

        <Space size="large" align="start" wrap>
          <Form.Item label="Category" name="categoryId" rules={[{ required: true, message: 'Choose a subject category.' }]}>
            <Select
              options={(choices.data?.categories ?? []).map((c) => ({ value: c.id, label: c.name }))}
              loading={choices.isPending}
              placeholder="Choose a category"
              style={{ width: 240 }}
            />
          </Form.Item>
          <Form.Item label="Owner" name="expertId" extra="The Expert who prepares the content. May stay empty and be assigned later.">
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              options={(choices.data?.experts ?? []).map((u) => ({ value: u.id, label: u.name }))}
              loading={choices.isPending}
              placeholder="Not assigned"
              style={{ width: 320 }}
            />
          </Form.Item>
        </Space>

        <Space size="large" align="start" wrap>
          <Form.Item label="Featured" name="isFeatured" valuePropName="checked" extra="Shown on the Home Page and in the sider of the subject pages.">
            <Switch />
          </Form.Item>
          <Form.Item
            label="Pass Rate"
            name="passRate"
            rules={[{ required: true, message: 'Enter the pass rate.' }]}
            extra="A practice passes at or above it; a new simulation exam starts from it (BR-21)."
          >
            <InputNumber min={0} max={100} precision={0} suffix="%" style={{ width: 120 }} />
          </Form.Item>
          <Form.Item label="Status" extra="A new subject stays Unpublished until the Admin publishes it.">
            <Tag>Unpublished</Tag>
          </Form.Item>
        </Space>

        <Form.Item label="Description" name="description" extra="Shown on Subject Details of the public pages.">
          <RichTextEditor />
        </Form.Item>

        <Space>
          <Button type="primary" htmlType="submit" loading={create.isPending}>
            Save
          </Button>
          <Button onClick={() => navigate(backTo)}>Cancel</Button>
        </Space>
      </Form>
    </>
  )
}

