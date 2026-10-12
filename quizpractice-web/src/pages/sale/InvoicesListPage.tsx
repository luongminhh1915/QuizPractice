import { Button, DatePicker, Flex, Input, Select, Space, Typography } from 'antd'
import dayjs from 'dayjs'
import { Link, useLocation } from 'react-router'
import type { InvoiceStatus } from '../../api/customerApi'
import type { SaleInvoiceSummary } from '../../api/saleApi'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useListParams } from '../../hooks/useListParams'
import { useInvoiceList } from '../../hooks/useSale'
import { formatVnd } from '../../utils/money'

const time = (value: string) => dayjs(value).format('DD/MM/YYYY HH:mm')

/**
 * Invoices List (SRS III 8.3; UC-68; team extension, OI-09): every invoice, the unpaid ones
 * first, so the Sale user can find the invoice of a transfer seen on the receiving account.
 */
export function InvoicesListPage() {
  const location = useLocation()
  const list = useListParams()
  const back = { state: { back: location.pathname + location.search } }
  const status = list.get('status') as InvoiceStatus | undefined
  const from = list.get('from')
  const to = list.get('to')
  const query = useInvoiceList({ ...list.criteria, status, from, to })

  const columns: DataTableColumn<SaleInvoiceSummary>[] = [
    { title: 'Transfer Code', key: 'code', width: 130, render: (i) => <Typography.Text strong copyable>{i.code}</Typography.Text> },
    {
      title: 'Customer',
      key: 'customerEmail',
      render: (i) => (
        <Flex vertical>
          <span>{i.customerEmail}</span>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {time(i.createdAt)}
          </Typography.Text>
        </Flex>
      ),
    },
    { title: 'Registrations', key: 'registrationCount', width: 120 },
    { title: 'Amount', key: 'totalAmount', width: 130, render: (i) => formatVnd(i.totalAmount) },
    { title: 'Status', key: 'status', width: 110, render: (i) => <StatusTag status={i.status} /> },
    {
      title: 'Confirmed',
      key: 'paidAt',
      width: 200,
      render: (i) =>
        i.paidAt ? (
          <Flex vertical style={{ fontSize: 12 }}>
            <span>{time(i.paidAt)}</span>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {i.confirmedBy}
            </Typography.Text>
          </Flex>
        ) : (
          '—'
        ),
    },
    {
      title: 'Action',
      key: 'action',
      width: 80,
      render: (i) => (
        <Link to={`/manage/invoices/${i.id}`} {...back}>
          <Button size="small">View</Button>
        </Link>
      ),
    },
  ]

  return (
    <>
      <PageHeader title="Invoices List" />

      <Space wrap style={{ marginBottom: 16 }}>
        <Input.Search
          key={list.criteria.search ?? ''}
          defaultValue={list.criteria.search}
          placeholder="Transfer code or email"
          allowClear
          onSearch={(value) => list.set({ search: value.trim() })}
          style={{ width: 260 }}
        />
        <DatePicker.RangePicker
          aria-label="Created"
          format="DD/MM/YYYY"
          value={from && to ? [dayjs(from), dayjs(to)] : null}
          onChange={(range) =>
            list.set({ from: range?.[0]?.format('YYYY-MM-DD'), to: range?.[1]?.format('YYYY-MM-DD') })
          }
          placeholder={['Created from', 'to']}
        />
        <Select
          aria-label="Status"
          value={status ?? 'All'}
          onChange={(value) => list.set({ status: value === 'All' ? undefined : value })}
          options={['All', 'Unpaid', 'Paid', 'Cancelled'].map((v) => ({ value: v, label: v === 'All' ? 'All statuses' : v }))}
          style={{ width: 140 }}
        />
      </Space>

      {query.error && <Typography.Paragraph type="danger">{errorMessage(query.error)}</Typography.Paragraph>}

      <DataTable
        columns={columns}
        page={query.data}
        loading={query.isFetching}
        criteria={list.criteria}
        onChange={list.set}
        hasCriteria={list.hasCriteria}
        onClearCriteria={list.clear}
      />
    </>
  )
}
