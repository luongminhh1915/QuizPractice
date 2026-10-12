import { useEffect } from 'react'
import { Alert, App, Button, Card, Col, Form, Input, QRCode, Row, Select, Space, Spin, Typography } from 'antd'
import dayjs from 'dayjs'
import { useNavigate } from 'react-router'
import type { BankAccountInput } from '../../api/bankAccountApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { useBankAccount, useSampleQr, useSaveBankAccount } from '../../hooks/useBankAccount'
import { formatVnd } from '../../utils/money'

/**
 * Bank Account Settings (SRS III 9.5; UC-70 Edit Receiving Bank Account; OI-09). The one
 * account every new invoice is paid to (BR-24); reached from Settings List.
 */
export function BankAccountPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const [form] = Form.useForm<BankAccountInput>()
  const account = useBankAccount()
  const save = useSaveBankAccount()

  const a = account.data
  useEffect(() => {
    if (a) form.setFieldsValue({ bankBin: a.bankBin ?? undefined, accountNumber: a.accountNumber ?? '', accountName: a.accountName ?? '' })
  }, [a, form])

  // Step 6: the sample follows the values on screen, saved or not.
  const bin = Form.useWatch('bankBin', form) ?? a?.bankBin ?? undefined
  const number = Form.useWatch('accountNumber', form) ?? a?.accountNumber ?? undefined
  const sample = useSampleQr(bin, number?.trim())

  if (account.isPending) return <Spin />
  if (account.isError) return <Typography.Paragraph type="danger">{errorMessage(account.error)}</Typography.Paragraph>

  const onFinish = (values: BankAccountInput) =>
    save.mutate(
      { input: values, version: a?.version ?? null },
      {
        onSuccess: (saved) =>
          saved.changed
            ? message.success('The receiving account was saved. New invoices are paid to it; invoices already made keep theirs.')
            : message.info('Nothing has changed, so nothing was stored.'),
        onError: (e) => {
          // E2: another Admin saved meanwhile; show the current values and ask for the change again.
          if (problemOf(e)?.code === 'BankAccount.Changed') {
            message.warning(errorMessage(e), 8)
            void account.refetch()
            return
          }
          // E1: keep the entry and mark the field.
          if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
        },
      },
    )

  return (
    <>
      <PageHeader title="Bank Account Settings" />
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 24, maxWidth: 960 }}
        title="Customers pay every new invoice by QR code to this one account. An invoice already made keeps the account it was made with."
      />
      <Row gutter={32}>
        <Col xs={24} md={14}>
          <Form<BankAccountInput> form={form} name="bank" layout="vertical" onFinish={onFinish} style={{ maxWidth: 520 }}>
            <Form.Item label="Bank" name="bankBin" rules={[{ required: true, message: 'Choose the bank from the list.' }]}>
              <Select
                showSearch
                optionFilterProp="label"
                placeholder="Choose a bank"
                options={(a?.banks ?? []).map((b) => ({ value: b.bin, label: `${b.shortName} — ${b.name}` }))}
              />
            </Form.Item>
            <Form.Item
              label="Account Number"
              name="accountNumber"
              rules={[
                { required: true, message: 'Enter the account number.' },
                { pattern: /^[0-9]{6,19}$/, message: 'Digits only, 6 to 19 of them.' },
              ]}
            >
              <Input inputMode="numeric" />
            </Form.Item>
            <Form.Item
              label="Account Holder"
              name="accountName"
              rules={[{ required: true, whitespace: true, message: 'Enter the account holder.' }, { max: 100 }]}
              extra="As the bank holds it; it is stored in capitals without accents, as banking apps show it."
            >
              <Input />
            </Form.Item>
            <Form.Item label="Last Updated">
              <Typography.Text type="secondary">
                {a?.updatedAt ? dayjs(a.updatedAt).format('DD/MM/YYYY HH:mm') : 'Not set yet'}
              </Typography.Text>
            </Form.Item>
            <Space>
              <Button type="primary" htmlType="submit" loading={save.isPending}>
                Save
              </Button>
              <Button onClick={() => navigate('/manage/settings')}>Cancel</Button>
            </Space>
          </Form>
        </Col>
        <Col xs={24} md={10}>
          <Card title="Sample QR" size="small" style={{ maxWidth: 320 }}>
            {sample.data ? (
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8 }}>
                <QRCode value={sample.data.payload} size={220} errorLevel="M" bordered={false} />
                <Typography.Text type="secondary" style={{ textAlign: 'center' }}>
                  {formatVnd(sample.data.amount)} · "{sample.data.content}". Scan it with a banking app and check the account name it shows. Do not
                  complete the transfer.
                </Typography.Text>
              </div>
            ) : (
              <Typography.Text type="secondary">Choose the bank and enter the account number to see a sample.</Typography.Text>
            )}
          </Card>
        </Col>
      </Row>
    </>
  )
}
