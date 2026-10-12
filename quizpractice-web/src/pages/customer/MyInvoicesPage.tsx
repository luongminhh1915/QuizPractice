import { Button, Empty, Flex, Result, Select, Table, Typography, type TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import { Link, useNavigate } from 'react-router'
import type { InvoiceStatus, InvoiceSummary } from '../../api/customerApi'
import { errorMessage } from '../../api/client'
import { StatusTag } from '../../components/StatusTag'
import { CategoriesBlock, ContactsBlock, SearchBlock } from '../../components/public/SiderBlocks'
import { PAGE_SIZE, useListParams } from '../../hooks/useListParams'
import { useMyInvoices } from '../../hooks/useCustomer'
import { useSubjectSider } from '../../hooks/usePublic'
import { PublicSider } from '../../layouts/PublicLayout'
import { formatVnd } from '../../utils/money'

/**
 * My Invoices (SRS III 3.5; UC-67; team extension, OI-09): the invoices of the account,
 * the newest first, filtered by status. View opens Invoice QR (2.7), with the QR code
 * again for an Unpaid invoice and read only otherwise.
 */
export function MyInvoicesPage() {
  const navigate = useNavigate()
  const { criteria, get, set } = useListParams()
  const status = get('status') as InvoiceStatus | undefined
  const list = useMyInvoices(status, criteria.page, PAGE_SIZE)
  const sider = useSubjectSider()

  const columns: TableColumnsType<InvoiceSummary> = [
    { title: 'Transfer Code', render: (_, i) => <Link to={`/my-invoices/${i.id}`}>{i.code}</Link> },
    { title: 'Created', render: (_, i) => dayjs(i.createdAt).format('DD/MM/YYYY HH:mm') },
    { title: 'Registrations', dataIndex: 'registrationCount', align: 'right' },
    { title: 'Amount', render: (_, i) => formatVnd(i.totalAmount), align: 'right' },
    { title: 'Status', render: (_, i) => <StatusTag status={i.status} /> },
    { title: 'Confirmed', render: (_, i) => (i.paidAt ? dayjs(i.paidAt).format('DD/MM/YYYY HH:mm') : '—') },
    {
      title: 'Action',
      render: (_, i) => (
        <Button size="small" onClick={() => navigate(`/my-invoices/${i.id}`)}>
          View
        </Button>
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
          My Invoices
        </Typography.Title>
        <Select<InvoiceStatus | 'All'>
          style={{ width: 160 }}
          value={status ?? 'All'}
          onChange={(v) => set({ status: v === 'All' ? undefined : v })}
          options={['All', 'Unpaid', 'Paid', 'Cancelled'].map((v) => ({ value: v as InvoiceStatus | 'All', label: v === 'All' ? 'All statuses' : v }))}
        />
      </Flex>
      {list.isError ? (
        <Result status="warning" title={errorMessage(list.error)} />
      ) : (
        <Table<InvoiceSummary>
          rowKey="id"
          size="middle"
          loading={list.isPending}
          columns={columns}
          dataSource={list.data?.items}
          scroll={{ x: 700 }}
          locale={{
            // A1: invoices are made from My Registrations.
            emptyText: status ? (
              <Empty description={`No ${status.toLowerCase()} invoice.`} />
            ) : (
              <Empty description="You have no invoice yet.">
                <Button type="primary" onClick={() => navigate('/my-registrations')}>
                  My Registrations
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
