import { useState } from 'react'
import { App, Button, Empty, Flex, Popconfirm, Result, Space, Table, Typography, type TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import { Link, useNavigate } from 'react-router'
import type { MyRegistration } from '../../api/customerApi'
import { errorMessage } from '../../api/client'
import { StatusTag } from '../../components/StatusTag'
import { SubjectRegisterModal } from '../../components/public/SubjectRegisterModal'
import { CategoriesBlock, ContactsBlock, SearchBlock } from '../../components/public/SiderBlocks'
import { PAGE_SIZE, useListParams } from '../../hooks/useListParams'
import { useCancelRegistration, useCreateInvoice, useMyRegistrations } from '../../hooks/useCustomer'
import { useSubjectSider } from '../../hooks/usePublic'
import { PublicSider } from '../../layouts/PublicLayout'
import { formatVnd } from '../../utils/money'

/**
 * My Registrations (SRS III 3.1; F#13; UC-15): every registration of the account, the
 * newest first. The sider searches the catalog (it opens Subjects List).
 */
export function MyRegistrationsPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const { criteria, set } = useListParams()
  const list = useMyRegistrations(criteria.page, PAGE_SIZE)
  const sider = useSubjectSider()
  const [editing, setEditing] = useState<number>()
  const cancel = useCancelRegistration()
  const pay = useCreateInvoice()
  const [selected, setSelected] = useState<number[]>([])

  // UC-18: one invoice for the ticked registrations, then its QR code.
  const paySelected = () =>
    pay.mutate(selected, {
      onSuccess: (invoice) => {
        setSelected([])
        navigate(`/my-invoices/${invoice.id}`)
      },
      // E1 names the registration; the list reloads to the current state.
      onError: (e) => {
        setSelected([])
        message.warning(errorMessage(e))
      },
    })
  const selectedTotal = (list.data?.items ?? []).filter((r) => selected.includes(r.id)).reduce((sum, r) => sum + r.totalCost, 0)

  const columns: TableColumnsType<MyRegistration> = [
    {
      title: 'Registration',
      // Number, subject and registration time in one cell, so the actions stay in view.
      render: (_, r) => (
        <Flex vertical>
          <Link to={`/subjects/${r.subjectId}`}>{r.subjectName}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            #{r.id} · {dayjs(r.registeredAt).format('DD/MM/YYYY HH:mm')}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: 'Package',
      render: (_, r) => (
        <Flex vertical>
          <span>{r.packageName}</span>
          <Typography.Text strong>{formatVnd(r.totalCost)}</Typography.Text>
        </Flex>
      ),
    },
    {
      title: 'Status',
      // The unpaid invoice it waits in, under the status; its code opens Invoice QR (BR-24).
      render: (_, r) => (
        <Flex vertical gap={4} align="start">
          <StatusTag status={r.status} />
          {r.unpaidInvoice && (
            <Link to={`/my-invoices/${r.unpaidInvoice.id}`} style={{ fontSize: 12 }}>
              Invoice {r.unpaidInvoice.code}
            </Link>
          )}
        </Flex>
      ),
    },
    {
      title: 'Validity',
      // Set when the registration becomes Paid (BR-14); a dash before that.
      render: (_, r) =>
        r.validFrom ? (
          <Flex vertical>
            <span>{dayjs(r.validFrom).format('DD/MM/YYYY')}</span>
            <span>{dayjs(r.validTo).format('DD/MM/YYYY')}</span>
          </Flex>
        ) : (
          '—'
        ),
    },
    {
      title: 'Action',
      // Submitted and in no unpaid invoice only (BR-18, BR-24).
      render: (_, r) =>
        r.canChange && (
          <Space size={4}>
            <Button size="small" onClick={() => setEditing(r.id)}>
              Edit
            </Button>
            {/* UC-17: confirm first; without confirmation nothing changes (A1). */}
            <Popconfirm
              title="Cancel this registration?"
              description="It will no longer wait for the Sale team."
              okText="Cancel registration"
              cancelText="Keep it"
              onConfirm={() =>
                cancel.mutateAsync(r.id).then(
                  () => message.success(`Registration ${r.id} was cancelled.`),
                  (e) => message.warning(errorMessage(e)),
                )
              }
            >
              <Button size="small" danger>
                Cancel
              </Button>
            </Popconfirm>
          </Space>
        ),
    },
  ]

  return (
    <PublicSider
      blocks={
        <>
          <SearchBlock placeholder="Search subjects by name" onSearch={(t) => navigate(t ? `/subjects?search=${encodeURIComponent(t)}` : '/subjects')} />
          <CategoriesBlock categories={sider.data?.categories ?? []} onSelect={(c) => navigate(c ? `/subjects?categoryId=${c}` : '/subjects')} />
          <ContactsBlock />
        </>
      }
    >
      <Flex justify="space-between" align="center" wrap gap={8} style={{ marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          My Registrations
        </Typography.Title>
        <Space>
          {!!selected.length && <Typography.Text>Selected: {formatVnd(selectedTotal)}</Typography.Text>}
          <Button type="primary" disabled={!selected.length} loading={pay.isPending} onClick={paySelected}>
            Pay Selected by QR
          </Button>
        </Space>
      </Flex>
      {list.isError ? (
        <Result status="warning" title={errorMessage(list.error)} />
      ) : (
        <Table<MyRegistration>
          rowKey="id"
          size="middle"
          loading={list.isPending}
          columns={columns}
          dataSource={list.data?.items}
          // Only a Submitted registration in no unpaid invoice can be ticked (BR-24).
          rowSelection={{
            selectedRowKeys: selected,
            onChange: (keys) => setSelected(keys as number[]),
            getCheckboxProps: (r) => ({ disabled: !r.canChange }),
          }}
          scroll={{ x: 700 }}
          locale={{
            // A1: nothing yet; the catalog is where registrations start.
            emptyText: (
              <Empty description="You have not registered for any subject yet.">
                <Button type="primary" onClick={() => navigate('/subjects')}>
                  Browse the subjects
                </Button>
              </Empty>
            ),
          }}
          pagination={{
            current: list.data?.pageNumber ?? criteria.page,
            pageSize: PAGE_SIZE,
            total: list.data?.totalCount ?? 0,
            hideOnSinglePage: true,
            showSizeChanger: false,
            onChange: (page) => set({ page }),
          }}
        />
      )}
      {editing !== undefined && (
        <SubjectRegisterModal target={{ registrationId: editing }} open onClose={() => setEditing(undefined)} />
      )}
    </PublicSider>
  )
}
