import { useEffect } from 'react'
import { App, Button, Form, Input, InputNumber, Select, Space, Switch } from 'antd'
import { useNavigate } from 'react-router'
import type { Subject, SubjectOverviewInput } from '../../api/subjectsApi'
import { applyFieldErrors, errorMessage } from '../../api/client'
import { ImageUpload } from '../../components/ImageUpload'
import { RichTextEditor } from '../../components/RichTextEditor'
import { StatusTag } from '../../components/StatusTag'
import { BRIEF_MAX, NAME_MAX } from '../../constants/fieldLength'
import { useSubjectChoices, useUpdateSubject } from '../../hooks/useSubjects'

/**
 * Overview tab of Subject Management Details (SRS III 6.3; UC-44 Edit Subject Details): the
 * general information, which the Admin and the assigned Expert may change.
 */
export function SubjectOverviewTab({ subject, backTo }: { subject: Subject; backTo: string }) {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const [form] = Form.useForm<SubjectOverviewInput>()
  const choices = useSubjectChoices()
  const update = useUpdateSubject(subject.id)

  useEffect(() => {
    const { name, tagline, brief, categoryId, isFeatured, passRate, description } = subject
    form.setFieldsValue({ name, tagline: tagline ?? '', brief, categoryId, isFeatured, passRate, description, thumbnail: undefined })
  }, [subject, form])

  // Active categories can be chosen; one switched off since stays on the subject that has it (BR-16).
  const categories = choices.data?.categories ?? []
  const categoryOptions = [
    ...categories.map((c) => ({ value: c.id, label: c.name })),
    ...(categories.some((c) => c.id === subject.categoryId)
      ? []
      : [{ value: subject.categoryId, label: `${subject.category} (no longer in use)` }]),
  ]

  const onFinish = (values: SubjectOverviewInput) =>
    update.mutate(values, {
      onSuccess: (saved) => message.success(`Subject "${saved.name}" was saved.`),
      // E2: keep everything entered and mark the fields to correct.
      onError: (e) => {
        if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
      },
    })

  return (
    <Form<SubjectOverviewInput> form={form} layout="vertical" onFinish={onFinish} style={{ maxWidth: 880 }}>
      <Form.Item label="Thumbnail" name="thumbnail" extra="JPG or PNG, up to 2 MB. Leave it to keep the current image.">
        <ImageUpload currentUrl={subject.thumbnail} width={280} />
      </Form.Item>

      <Form.Item label="Name" name="name" rules={[{ required: true, whitespace: true, message: 'Enter the subject name.' }, { max: NAME_MAX }]}>
        <Input />
      </Form.Item>

      <Space size="large" align="start" wrap>
        <Form.Item label="Category" name="categoryId" rules={[{ required: true, message: 'Choose a subject category.' }]}>
          <Select options={categoryOptions} loading={choices.isPending} style={{ width: 240 }} />
        </Form.Item>
        <Form.Item label="Owner" extra="Changed by the Admin only.">
          <Input aria-label="Owner" value={subject.owner ?? 'Not assigned'} disabled style={{ width: 320 }} />
        </Form.Item>
      </Space>

      <Form.Item label="Tag Line" name="tagline" rules={[{ max: BRIEF_MAX }]} extra="One line under the name on the public pages.">
        <Input />
      </Form.Item>
      <Form.Item label="Brief Information" name="brief" rules={[{ max: BRIEF_MAX }]} extra="Short introduction on Subject Details.">
        <Input.TextArea rows={3} showCount maxLength={BRIEF_MAX} />
      </Form.Item>

      <Space size="large" align="start" wrap>
        <Form.Item label="Featured" name="isFeatured" valuePropName="checked">
          <Switch />
        </Form.Item>
        <Form.Item
          label="Pass Rate"
          name="passRate"
          rules={[{ required: true, message: 'Enter the pass rate.' }]}
          extra="Applies to attempts scored from now on; past results keep theirs (BR-21)."
        >
          <InputNumber min={0} max={100} precision={0} suffix="%" style={{ width: 120 }} />
        </Form.Item>
        <Form.Item label="Status" extra="Published by the Admin only.">
          <StatusTag status={subject.status} />
        </Form.Item>
      </Space>

      <Form.Item label="Description" name="description" extra="Shown on Subject Details of the public pages.">
        <RichTextEditor />
      </Form.Item>

      <Space>
        <Button type="primary" htmlType="submit" loading={update.isPending}>
          Save
        </Button>
        <Button onClick={() => navigate(backTo)}>Cancel</Button>
      </Space>
    </Form>
  )
}
