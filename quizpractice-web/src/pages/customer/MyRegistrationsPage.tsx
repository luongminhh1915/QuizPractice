import { Button, Empty, Flex, Result, Table, Typography, type TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import { Link, useNavigate } from 'react-router'
import type { MyRegistration } from '../../api/customerApi'
import { errorMessage } from '../../api/client'
import { StatusTag } from '../../components/StatusTag'
import { CategoriesBlock, ContactsBlock, SearchBlock } from '../../components/public/SiderBlocks'
import { PAGE_SIZE, useListParams } from '../../hooks/useListParams'
import { useMyRegistrations } from '../../hooks/useCustomer'
import { useSubjectSider } from '../../hooks/usePublic'
import { PublicSider } from '../../layouts/PublicLayout'
import { formatVnd } from '../../utils/money'

/**
 * My Registrations (SRS III 3.1; F#13; UC-15): every registration of the account, the
 * newest first. The sider searches the catalog (it opens Subjects List).
 */
export function MyRegistrationsPage() {
  const navigate = useNavigate()
  const { criteria, set } = useListParams()
  const list = useMyRegistrations(criteria.page, PAGE_SIZE)
  const sider = useSubjectSider()

  const columns: TableColumnsType<MyRegistration> = [
    { title: 'ID', dataIndex: 'id', width: 64 },
    {
      title: 'Subject',
      render: (_, r) => <Link to={`/subjects/${r.subjectId}`}>{r.subjectName}</Link>,
    },
    { title: 'Package', dataIndex: 'packageName' },
    { title: 'Total Cost', render: (_, r) => formatVnd(r.totalCost), align: 'right' },
    { title: 'Registered', render: (_, r) => dayjs(r.registeredAt).format('DD/MM/YYYY HH:mm') },
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
      render: (_, r) => (r.validFrom ? `${dayjs(r.validFrom).format('DD/MM/YYYY')} – ${dayjs(r.validTo).format('DD/MM/YYYY')}` : '—'),
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
      <Typography.Title level={3}>My Registrations</Typography.Title>
      {list.isError ? (
        <Result status="warning" title={errorMessage(list.error)} />
      ) : (
        <Table<MyRegistration>
          rowKey="id"
          size="middle"
          loading={list.isPending}
          columns={columns}
          dataSource={list.data?.items}
          scroll={{ x: 900 }}
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
    </PublicSider>
  )
}
