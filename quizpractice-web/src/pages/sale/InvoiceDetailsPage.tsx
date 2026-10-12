import { useState } from 'react'
import { Alert, App, Button, Card, Descriptions, Flex, Input, Result, Space, Spin, Table, Typography } from 'antd'
import { CheckOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import type { SaleInvoiceDetails } from '../../api/saleApi'
import { errorMessage, problemOf } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { BRIEF_MAX } from '../../constants/fieldLength'
import { useSaleInvoice, useSettleInvoice } from '../../hooks/useSale'
import { formatVnd } from '../../utils/money'

const time = (value: string) => dayjs(value).format('DD/MM/YYYY HH:mm')

/**
 * Invoice Details (SRS III 8.4; UC-69; team extension, OI-09). The Sale user looks on the
 * receiving bank account, outside the system, for a transfer with this code and amount,
 * then confirms: the invoice and every registration in it become Paid (BR-14, BR-19).
 */
export function InvoiceDetailsPage() {
  const { message, modal } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const id = Number(useParams().id)
  const invoice = useSaleInvoice(id)
  const settle = useSettleInvoice(id)
  // What the Sale user typed; until then the stored notes show.
  const [draft, setDraft] = useState<string>()
  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/invoices'

  if (invoice.isPending) return <Spin />
  if (invoice.isError) {
    const notFound = problemOf(invoice.error)?.status === 404
    return (
      <Result
        status={notFound ? '404' : 'error'}
        title={notFound ? 'This invoice does not exist.' : errorMessage(invoice.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Invoices List</Button>}
      />
    )
  }

  const i: SaleInvoiceDetails = invoice.data
  const unpaid = i.status === 'Unpaid'
  const notes = draft ?? i.notes ?? ''

  const run = (confirm: boolean) =>
    settle.mutate(
      { confirm, notes: notes || undefined },
      {
        onSuccess: (saved) => {
          setDraft(undefined)
          message.success(confirm ? `Invoice ${saved.code} is Paid; its registrations are Paid.` : 'The notes were saved.')
        },
        // E1, E2, E3: say why, and the invoice reloads with its current status.
        onError: (e) => message.error(errorMessage(e), 6),
      },
    )

  const confirmPaid = () =>
    modal.confirm({
      title: `Confirm the payment of ${i.code}?`,
      content: `Only after finding a transfer of ${formatVnd(i.totalAmount)} with the content ${i.code} on the receiving account. Its ${i.registrations.length} registration(s) become Paid and open from today.`,
      okText: 'Confirm Paid',
      onOk: () => run(true),
    })

  return (
    <>
      <PageHeader
        title={`Invoice ${i.code}`}
        action={
          <Space>
            <StatusTag status={i.status} />
          </Space>
        }
      />
      <Flex vertical gap={16}>
        <Card>
          <Descriptions column={{ xs: 1, md: 2 }} size="small" bordered>
            <Descriptions.Item label="Transfer Code">
              <Typography.Text strong copyable>
                {i.code}
              </Typography.Text>
            </Descriptions.Item>
            <Descriptions.Item label="Amount">
              <Typography.Text strong>{formatVnd(i.totalAmount)}</Typography.Text>
            </Descriptions.Item>
            <Descriptions.Item label="Customer">
              {i.customerName} · {i.customerEmail}
            </Descriptions.Item>
            <Descriptions.Item label="Created">{time(i.createdAt)}</Descriptions.Item>
            <Descriptions.Item label="Receiving Account" span={2}>
              {i.bankName} · {i.accountNumber} · {i.accountName}
            </Descriptions.Item>
            {i.paidAt && (
              <Descriptions.Item label="Confirmed" span={2}>
                {time(i.paidAt)} by {i.confirmedBy}
              </Descriptions.Item>
            )}
          </Descriptions>
          {unpaid ? (
            <Alert
              style={{ marginTop: 12 }}
              type="info"
              showIcon
              title="Check the receiving account outside the system for a transfer with this code and amount before confirming (BR-19)."
            />
          ) : (
            <Alert
              style={{ marginTop: 12 }}
              type={i.status === 'Paid' ? 'success' : 'warning'}
              showIcon
              title={i.status === 'Paid' ? 'Confirmed: read only, only the notes can be added.' : 'Cancelled by the customer: read only, only the notes can be added.'}
            />
          )}
        </Card>

        <Card title="Registrations">
          <Table
            rowKey="id"
            size="small"
            pagination={false}
            dataSource={i.registrations}
            columns={[
              {
                title: 'ID',
                dataIndex: 'id',
                width: 80,
                render: (rid: number) => <Link to={`/manage/registrations/${rid}`}>#{rid}</Link>,
              },
              { title: 'Subject', dataIndex: 'subjectName' },
              { title: 'Package', dataIndex: 'packageName' },
              { title: 'Cost', dataIndex: 'totalCost', align: 'right', render: (v: number) => formatVnd(v) },
              { title: 'Status', dataIndex: 'status', width: 110, render: (s: string) => <StatusTag status={s} /> },
            ]}
          />
        </Card>

        <Card title="Notes">
          <Input.TextArea
            aria-label="Notes"
            rows={3}
            maxLength={BRIEF_MAX}
            showCount
            value={notes}
            onChange={(e) => setDraft(e.target.value)}
            placeholder="Where the transfer was found, or what did not match (UC-69 A1, A2)"
          />
        </Card>

        <Flex gap={8} justify="end">
          <Button onClick={() => navigate(backTo)}>Back</Button>
          <Button loading={settle.isPending && !settle.variables?.confirm} onClick={() => run(false)}>
            Save Notes
          </Button>
          {unpaid && (
            <Button type="primary" icon={<CheckOutlined />} loading={settle.isPending && settle.variables?.confirm} onClick={confirmPaid}>
              Confirm Paid
            </Button>
          )}
        </Flex>
      </Flex>
    </>
  )
}
