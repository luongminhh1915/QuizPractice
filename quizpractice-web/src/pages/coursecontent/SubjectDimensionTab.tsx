import { useState } from 'react'
import { App, Button, Form, Input, Modal, Popconfirm, Select, Space, Table, Typography } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import type { Dimension, DimensionInput } from '../../api/subjectsApi'
import { applyFieldErrors, errorMessage } from '../../api/client'
import { NAME_MAX } from '../../constants/fieldLength'
import { useDeleteDimension, useDimensions, useSaveDimension } from '../../hooks/useSubjects'

/**
 * Dimension tab of Subject Management Details (SRS III 6.4; F#26; UC-46): the dimensions the
 * questions of the subject are tagged with and a practice may draw by.
 */
export function SubjectDimensionTab({ subjectId }: { subjectId: number }) {
  const { message } = App.useApp()
  const tab = useDimensions(subjectId)
  const save = useSaveDimension(subjectId)
  const remove = useDeleteDimension(subjectId)
  const [form] = Form.useForm<DimensionInput>()
  // undefined: form closed; null: a new dimension; otherwise the one being edited.
  const [editing, setEditing] = useState<Dimension | null>()

  const open = (dimension: Dimension | null) => {
    setEditing(dimension)
    form.resetFields()
    if (dimension) form.setFieldsValue({ typeId: dimension.typeId, name: dimension.name, description: dimension.description ?? '' })
  }

  const onFinish = (values: DimensionInput) =>
    save.mutate(
      { id: editing?.id, input: values },
      {
        onSuccess: (saved) => {
          message.success(`Dimension "${saved.name}" was saved.`)
          setEditing(undefined)
        },
        // E2: keep the entry and mark the field; a type in use by a quiz says why.
        onError: (e) => {
          if (!applyFieldErrors(form, e)) message.error(errorMessage(e), 8)
        },
      },
    )

  const deleteDimension = (d: Dimension) =>
    remove.mutate(d.id, {
      onSuccess: () => message.success(`Dimension "${d.name}" was deleted.`),
      // E3: the message says how many questions, practices and quiz rules keep it.
      onError: (e) => message.error(errorMessage(e), 8),
    })

  // A type switched off since stays selectable on the dimension that has it (BR-16).
  const types = tab.data?.types ?? []
  const typeOptions = [
    ...types.map((t) => ({ value: t.id, label: t.name })),
    ...(editing && !types.some((t) => t.id === editing.typeId) ? [{ value: editing.typeId, label: `${editing.type} (no longer in use)` }] : []),
  ]

  return (
    <>
      <Space style={{ marginBottom: 16 }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => open(null)}>
          Add New
        </Button>
      </Space>

      {tab.error && <Typography.Paragraph type="danger">{errorMessage(tab.error)}</Typography.Paragraph>}

      <Table<Dimension>
        rowKey="id"
        loading={tab.isFetching}
        dataSource={tab.data?.dimensions}
        pagination={false}
        locale={{ emptyText: 'No dimension yet. The questions of this subject are tagged with its dimensions.' }}
        columns={[
          { title: 'Type', dataIndex: 'type', width: 140 },
          { title: 'Name', dataIndex: 'name', width: 240 },
          { title: 'Description', dataIndex: 'description', render: (v: string | null) => v ?? '' },
          {
            title: 'Action',
            key: 'action',
            width: 160,
            render: (_, d) => (
              <Space>
                <Button size="small" onClick={() => open(d)}>
                  Edit
                </Button>
                <Popconfirm
                  title="Delete this dimension?"
                  description={`"${d.name}" is removed while no question, practice or quiz uses it.`}
                  okText="Delete"
                  okButtonProps={{ danger: true }}
                  cancelText="Cancel"
                  onConfirm={() => deleteDimension(d)}
                >
                  <Button size="small" danger loading={remove.isPending && remove.variables === d.id}>
                    Delete
                  </Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Edit Dimension' : 'New Dimension'}
        open={editing !== undefined}
        okText="Save"
        onOk={() => form.submit()}
        confirmLoading={save.isPending}
        onCancel={() => setEditing(undefined)}
        forceRender
      >
        <Form<DimensionInput> form={form} name="dimension" layout="vertical" onFinish={onFinish}>
          <Form.Item label="Type" name="typeId" rules={[{ required: true, message: 'Choose the dimension type.' }]}>
            <Select options={typeOptions} placeholder="Choose a type" />
          </Form.Item>
          <Form.Item label="Dimension Name" name="name" rules={[{ required: true, whitespace: true, message: 'Enter the dimension name.' }, { max: NAME_MAX }]}>
            <Input placeholder="Such as Inheritance" />
          </Form.Item>
          <Form.Item label="Dimension Description" name="description" extra="What the dimension covers, for the experts who tag the questions.">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
