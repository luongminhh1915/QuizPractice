import { Button, Form, Input, InputNumber, Select, Space, Switch } from 'antd'
import { useNavigate } from 'react-router'
import type { Subject } from '../../api/subjectsApi'
import { ImageUpload } from '../../components/ImageUpload'
import { RichTextEditor } from '../../components/RichTextEditor'
import { StatusTag } from '../../components/StatusTag'

/** Overview tab of Subject Management Details (SRS III 6.3): the general information of the subject. */
export function SubjectOverviewTab({ subject, backTo }: { subject: Subject; backTo: string }) {
  const navigate = useNavigate()

  return (
    <Form layout="vertical" disabled initialValues={subject} style={{ maxWidth: 880 }}>
      <Form.Item label="Thumbnail">
        <ImageUpload currentUrl={subject.thumbnail} width={280} />
      </Form.Item>

      <Form.Item label="Name" name="name">
        <Input />
      </Form.Item>

      <Space size="large" align="start" wrap>
        <Form.Item label="Category">
          <Select value={subject.categoryId} options={[{ value: subject.categoryId, label: subject.category }]} style={{ width: 240 }} />
        </Form.Item>
        <Form.Item label="Owner">
          <Input value={subject.owner ?? 'Not assigned'} style={{ width: 320 }} />
        </Form.Item>
      </Space>

      <Form.Item label="Tag Line" name="tagline">
        <Input />
      </Form.Item>
      <Form.Item label="Brief Information" name="brief">
        <Input.TextArea rows={3} />
      </Form.Item>

      <Space size="large" align="start" wrap>
        <Form.Item label="Featured" name="isFeatured" valuePropName="checked">
          <Switch />
        </Form.Item>
        <Form.Item label="Pass Rate" name="passRate">
          <InputNumber min={0} max={100} precision={0} suffix="%" style={{ width: 120 }} />
        </Form.Item>
        <Form.Item label="Status">
          <StatusTag status={subject.status} />
        </Form.Item>
      </Space>

      <Form.Item label="Description" name="description">
        <RichTextEditor />
      </Form.Item>

      <Space>
        <Button disabled={false} onClick={() => navigate(backTo)}>
          Back
        </Button>
      </Space>
    </Form>
  )
}
