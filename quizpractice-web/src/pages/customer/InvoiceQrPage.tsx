import { Alert, App, Button, Card, Col, Descriptions, Flex, Popconfirm, QRCode, Result, Row, Spin, Table, Typography } from 'antd'
import dayjs from 'dayjs'
import { Link, useNavigate, useParams } from 'react-router'
import { errorMessage, problemOf } from '../../api/client'
import { StatusTag } from '../../components/StatusTag'
import { useCancelInvoice, useMyInvoice } from '../../hooks/useCustomer'
import { formatVnd } from '../../utils/money'

/**
 * Invoice QR (SRS III 2.7; UC-18, UC-67; team extension, OI-09). The QR code fills the
 * account, the amount and the transfer code in any banking app. The system never sees
 * the transfer: the Sale role checks the account and confirms the invoice (BR-19).
 */
export function InvoiceQrPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const id = Number(useParams().id)
  const invoice = useMyInvoice(id)
  const cancel = useCancelInvoice(id)

  if (invoice.isPending) return <Spin style={{ display: 'block', padding: 48 }} />
  if (invoice.isError) {
    const missing = problemOf(invoice.error)?.status === 404
    return (
      <Result
        status={missing ? '404' : 'warning'}
        title={missing ? 'This invoice does not exist.' : errorMessage(invoice.error)}
        extra={<Button onClick={() => navigate('/my-invoices')}>My Invoices</Button>}
      />
    )
  }

  const inv = invoice.data
  const unpaid = inv.status === 'Unpaid' && inv.qrPayload
  return (
    <Flex vertical gap={16} style={{ maxWidth: 960, margin: '0 auto' }}>
      <Flex justify="space-between" align="center" wrap gap={8}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Invoice {inv.code} <StatusTag status={inv.status} />
        </Typography.Title>
        <Link to="/my-invoices">My Invoices</Link>
      </Flex>
      <Typography.Text type="secondary">
        Created {dayjs(inv.createdAt).format('DD/MM/YYYY HH:mm')}
        {inv.paidAt && ` · Confirmed ${dayjs(inv.paidAt).format('DD/MM/YYYY HH:mm')}`}
      </Typography.Text>

      {unpaid ? (
        <Row gutter={[16, 16]}>
          <Col xs={24} md={10}>
            <Card>
              <Flex vertical align="center" gap={12}>
                <QRCode value={inv.qrPayload!} size={240} errorLevel="M" bordered={false} />
                <Typography.Text type="secondary" style={{ textAlign: 'center' }}>
                  Scan with your banking app. The account, the amount and the transfer content are filled in for you.
                </Typography.Text>
              </Flex>
            </Card>
          </Col>
          <Col xs={24} md={14}>
            <Card title="Or transfer by hand">
              <Descriptions column={1} size="small">
                <Descriptions.Item label="Bank">{inv.bankName}</Descriptions.Item>
                <Descriptions.Item label="Account Number">
                  <Typography.Text copyable>{inv.accountNumber}</Typography.Text>
                </Descriptions.Item>
                <Descriptions.Item label="Account Holder">{inv.accountName}</Descriptions.Item>
                <Descriptions.Item label="Amount">
                  <Typography.Text copyable={{ text: String(inv.totalAmount) }} strong>
                    {formatVnd(inv.totalAmount)}
                  </Typography.Text>
                </Descriptions.Item>
                <Descriptions.Item label="Transfer Content">
                  <Typography.Text copyable strong>
                    {inv.code}
                  </Typography.Text>
                </Descriptions.Item>
              </Descriptions>
              {/* E3: a transfer without the code cannot be matched; the Sale role settles it by hand. */}
              <Alert
                type="info"
                showIcon
                style={{ marginTop: 12 }}
                title="Keep the transfer content exactly as shown. The Sale team confirms the payment after checking the account; your registrations then become Paid. The invoice stays in My Invoices until then."
              />
            </Card>
          </Col>
        </Row>
      ) : (
        <Alert
          type={inv.status === 'Paid' ? 'success' : 'warning'}
          showIcon
          title={inv.status === 'Paid' ? 'This invoice has been paid and confirmed by the Sale team.' : 'This invoice was cancelled. Its registrations can be paid again from My Registrations.'}
        />
      )}

      <Card title="Registrations">
        <Table
          rowKey="id"
          size="small"
          pagination={false}
          dataSource={inv.registrations}
          columns={[
            { title: 'ID', dataIndex: 'id', width: 64 },
            { title: 'Subject', dataIndex: 'subjectName' },
            { title: 'Package', dataIndex: 'packageName' },
            { title: 'Cost', align: 'right', render: (_, r) => formatVnd(r.totalCost) },
          ]}
          summary={() => (
            <Table.Summary.Row>
              <Table.Summary.Cell index={0} colSpan={3}>
                <strong>Total</strong>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={1} align="right">
                <strong>{formatVnd(inv.totalAmount)}</strong>
              </Table.Summary.Cell>
            </Table.Summary.Row>
          )}
        />
      </Card>

      {unpaid && (
        <Flex justify="end">
          {/* A2: cancelling frees the registrations, which may then be edited, cancelled or paid again. */}
          <Popconfirm
            title="Cancel this invoice?"
            description="Do not cancel it if you have already made the transfer."
            okText="Cancel invoice"
            cancelText="Keep it"
            onConfirm={() =>
              cancel.mutateAsync().then(
                () => message.success(`Invoice ${inv.code} was cancelled.`),
                (e) => message.warning(errorMessage(e)),
              )
            }
          >
            <Button danger>Cancel Invoice</Button>
          </Popconfirm>
        </Flex>
      )}
    </Flex>
  )
}
