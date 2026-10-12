import { useState } from 'react'
import { App, Button, Form, Input, InputNumber, Modal, Space, Table, Typography } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import type { PricePackage, PricePackageInput } from '../../api/subjectsApi'
import { applyFieldErrors, errorMessage } from '../../api/client'
import { StatusTag } from '../../components/StatusTag'
import { NAME_MAX } from '../../constants/fieldLength'
import { useChangePackageStatus, usePackages, useSavePackage } from '../../hooks/useSubjects'

/** Whole VND with thousands separators, as the public pages show prices. */
const vnd = (n: number) => `${n.toLocaleString('vi-VN')} ₫`

/** Months the access lasts (BR-14; the CHECK of SDS I.3). */
const MAX_MONTHS = 24

/**
 * Price Package tab of Subject Management Details (SRS III 6.5; F#27): the Admin adds, edits,
 * activates and deactivates packages (UC-47); the assigned Expert only views them (UC-48).
 */
export function SubjectPackageTab({ subjectId }: { subjectId: number }) {
  const { message } = App.useApp()
  const tab = usePackages(subjectId)
  const save = useSavePackage(subjectId)
  const changeStatus = useChangePackageStatus(subjectId)
  const [form] = Form.useForm<PricePackageInput>()
  // undefined: form closed; null: a new package; otherwise the one being edited.
  const [editing, setEditing] = useState<PricePackage | null>()
  const canEdit = tab.data?.canEdit ?? false

  const open = (pkg: PricePackage | null) => {
    setEditing(pkg)
    form.resetFields()
    if (pkg) {
      const { name, accessMonths, listPrice, salePrice, description } = pkg
      form.setFieldsValue({ name, accessMonths, listPrice, salePrice, description: description ?? '' })
    }
  }

  const onFinish = (values: PricePackageInput) =>
    save.mutate(
      { id: editing?.id, input: values },
      {
        onSuccess: (saved) => {
          message.success(`Package "${saved.name}" was saved.`)
          setEditing(undefined)
        },
        // E1: keep the entry and mark the field.
        onError: (e) => {
          if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
        },
      },
    )

  const toggle = (pkg: PricePackage) => {
    const status = pkg.status === 'Active' ? 'Inactive' : 'Active'
    changeStatus.mutate(
      { id: pkg.id, status },
      {
        onSuccess: () =>
          message.success(
            status === 'Active'
              ? `Package "${pkg.name}" is offered in Subject Register again.`
              : `Package "${pkg.name}" is no longer offered; registrations made under it keep their access.`,
          ),
        onError: (e) => message.error(errorMessage(e)),
      },
    )
  }

  return (
    <>
      {canEdit && (
        <Space style={{ marginBottom: 16 }}>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => open(null)}>
            Add New
          </Button>
        </Space>
      )}

      {tab.error && <Typography.Paragraph type="danger">{errorMessage(tab.error)}</Typography.Paragraph>}

      <Table<PricePackage>
        rowKey="id"
        loading={tab.isFetching}
        dataSource={tab.data?.packages}
        pagination={false}
        locale={{
          // UC-48 A1: an Expert learns who adds the packages.
          emptyText: canEdit ? 'No package yet. Add one so customers can register.' : 'No package yet. The Admin adds the price packages.',
        }}
        columns={[
          { title: 'Package Name', dataIndex: 'name' },
          { title: 'Access Duration', dataIndex: 'accessMonths', width: 140, render: (m: number) => `${m} month${m === 1 ? '' : 's'}` },
          { title: 'List Price', dataIndex: 'listPrice', width: 150, align: 'right', render: vnd },
          { title: 'Sale Price', dataIndex: 'salePrice', width: 150, align: 'right', render: vnd },
          { title: 'Status', dataIndex: 'status', width: 110, render: (s: string) => <StatusTag status={s} /> },
          ...(canEdit
            ? [
                {
                  title: 'Action',
                  key: 'action',
                  width: 190,
                  render: (_: unknown, p: PricePackage) => (
                    <Space>
                      <Button size="small" onClick={() => open(p)}>
                        Edit
                      </Button>
                      <Button
                        size="small"
                        danger={p.status === 'Active'}
                        loading={changeStatus.isPending && changeStatus.variables?.id === p.id}
                        onClick={() => toggle(p)}
                      >
                        {p.status === 'Active' ? 'Deactivate' : 'Activate'}
                      </Button>
                    </Space>
                  ),
                },
              ]
            : []),
        ]}
      />

      <Modal
        title={editing ? 'Edit Price Package' : 'New Price Package'}
        open={editing !== undefined}
        okText="Save"
        onOk={() => form.submit()}
        confirmLoading={save.isPending}
        onCancel={() => setEditing(undefined)}
        forceRender
      >
        <Form<PricePackageInput> form={form} name="package" layout="vertical" onFinish={onFinish}>
          <Form.Item label="Package Name" name="name" rules={[{ required: true, whitespace: true, message: 'Enter the package name.' }, { max: NAME_MAX }]}>
            <Input placeholder="Such as 6 months" />
          </Form.Item>
          <Form.Item
            label="Access Duration"
            name="accessMonths"
            rules={[{ required: true, message: 'Enter the access duration.' }]}
            extra="Whole months; valid-to is set from it when a registration becomes Paid (BR-14)."
          >
            <InputNumber min={1} max={MAX_MONTHS} precision={0} suffix="months" style={{ width: 160 }} />
          </Form.Item>
          <Space size="large" align="start">
            <Form.Item label="List Price" name="listPrice" rules={[{ required: true, message: 'Enter the list price.' }]}>
              <InputNumber min={0} precision={0} step={10000} suffix="₫" style={{ width: 180 }} />
            </Form.Item>
            <Form.Item
              label="Sale Price"
              name="salePrice"
              dependencies={['listPrice']}
              rules={[
                { required: true, message: 'Enter the sale price.' },
                ({ getFieldValue }) => ({
                  validator: (_, value: number | undefined) =>
                    value === undefined || value === null || value <= (getFieldValue('listPrice') ?? 0)
                      ? Promise.resolve()
                      : Promise.reject(new Error('The sale price cannot be above the list price.')),
                }),
              ]}
            >
              <InputNumber min={0} precision={0} step={10000} suffix="₫" style={{ width: 180 }} />
            </Form.Item>
          </Space>
          <Form.Item label="Package Description" name="description" extra="What the package offers, shown in Subject Register.">
            <Input.TextArea rows={3} />
          </Form.Item>
          <Form.Item label="Package Status" extra="A new package starts Active; Activate and Deactivate are on the list.">
            <StatusTag status={editing?.status ?? 'Active'} />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
