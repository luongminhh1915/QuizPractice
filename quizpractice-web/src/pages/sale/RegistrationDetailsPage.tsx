import { useEffect } from 'react'
import { Alert, App, Button, Card, Col, Descriptions, Flex, Form, Input, Radio, Result, Row, Select, Space, Spin, Typography } from 'antd'
import dayjs from 'dayjs'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import type { RegistrationDetails, RegistrationInput, SalePackage } from '../../api/saleApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { BRIEF_MAX } from '../../constants/fieldLength'
import { useRegistration, useRegistrationChoices, useSaveRegistration } from '../../hooks/useSale'
import { formatVnd } from '../../utils/money'

const day = (value: string | null) => (value ? dayjs(value).format('DD/MM/YYYY') : '—')

/**
 * Registration Details (SRS III 8.2; F#36; UC-60, UC-61). /manage/registrations/new records
 * a registration for a customer; /manage/registrations/:id opens a stored one. What can
 * change follows BR-22: the whole form for the Sale user who recorded it while it is
 * Submitted, the notes for any other.
 */
export function RegistrationDetailsPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const params = useParams()
  const id = params.id ? Number(params.id) : undefined
  const isNew = id === undefined

  const [form] = Form.useForm<RegistrationInput>()
  const stored = useRegistration(id)
  const choices = useRegistrationChoices()
  const save = useSaveRegistration(id)
  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/registrations'
  const r = stored.data
  // The stored values stand in until the form reports its own: after /new turns into /:id
  // the form remounts and useWatch starts empty.
  const subjectId = Form.useWatch('subjectId', form) ?? r?.subjectId
  const packageId = Form.useWatch('packageId', form) ?? r?.package.id
  const editAll = isNew || r?.canEditAll === true

  useEffect(() => {
    if (r) {
      form.setFieldsValue({
        subjectId: r.subjectId,
        packageId: r.package.id,
        fullName: r.fullName,
        gender: r.gender,
        email: r.email,
        mobile: r.mobile,
        notes: r.notes ?? undefined,
      })
    }
  }, [r, form])

  if (!isNew && stored.isPending) return <Spin />
  if (!isNew && stored.isError) {
    const notFound = problemOf(stored.error)?.status === 404
    return (
      <Result
        status={notFound ? '404' : 'error'}
        title={notFound ? 'This registration does not exist.' : errorMessage(stored.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Registrations List</Button>}
      />
    )
  }

  // A stored registration of a subject no longer on sale still shows its own subject and package.
  const subjects = choices.data?.subjects ?? []
  const subjectOptions = [
    ...subjects.map((s) => ({ value: s.id, label: s.name })),
    ...(r && !subjects.some((s) => s.id === r.subjectId) ? [{ value: r.subjectId, label: r.subjectName }] : []),
  ]
  const packages: SalePackage[] = subjects.find((s) => s.id === subjectId)?.packages ?? (r && r.subjectId === subjectId ? [r.package] : [])
  const chosen = packages.find((p) => p.id === packageId) ?? (r?.package.id === packageId ? r?.package : undefined)

  const onFinish = (values: RegistrationInput) =>
    save.mutate(
      { input: values, notesOnly: !editAll },
      {
        onSuccess: (saved) => {
          message.success(isNew ? `Registration #${saved.id} was recorded.` : `Registration #${saved.id} was saved.`)
          if (isNew) navigate(`/manage/registrations/${saved.id}`, { replace: true, state: location.state })
        },
        // E1 under the fields; BR-22, E2 and E3 in the message area, then the page reloads.
        onError: (e) => {
          if (!applyFieldErrors(form, e)) message.error(errorMessage(e), 6)
        },
      },
    )

  return (
    <>
      <PageHeader title={isNew ? 'New Registration' : `Registration #${id}`} />
      {r && <RecordInfo r={r} />}
      <Form<RegistrationInput>
        form={form}
        layout="vertical"
        onFinish={onFinish}
        initialValues={{ gender: 'Male' }}
        onValuesChange={(changed) => {
          // Another subject has other packages (UC-60 step 4).
          if ('subjectId' in changed) form.setFieldValue('packageId', undefined)
        }}
      >
        <Row gutter={24}>
          <Col xs={24} lg={12}>
            <Card title="Subject and Package" style={{ marginBottom: 16 }}>
              <Form.Item name="subjectId" label="Subject" rules={[{ required: true, message: 'Choose a subject.' }]}>
                <Select
                  showSearch
                  optionFilterProp="label"
                  disabled={!editAll}
                  loading={choices.isPending}
                  placeholder="Choose a subject"
                  options={subjectOptions}
                />
              </Form.Item>
              <Form.Item name="packageId" label="Package" rules={[{ required: true, message: 'Choose a package.' }]}>
                <Select
                  disabled={!editAll || subjectId === undefined}
                  placeholder="Choose a package"
                  options={packages.map((p) => ({ value: p.id, label: `${p.name} — ${formatVnd(p.salePrice)}` }))}
                />
              </Form.Item>
              <Descriptions column={3} size="small">
                <Descriptions.Item label="List Price">{chosen ? formatVnd(chosen.listPrice) : '—'}</Descriptions.Item>
                <Descriptions.Item label="Sale Price">{chosen ? formatVnd(chosen.salePrice) : '—'}</Descriptions.Item>
                <Descriptions.Item label="Total Cost">
                  <Typography.Text strong>{chosen ? formatVnd(chosen.salePrice) : '—'}</Typography.Text>
                </Descriptions.Item>
              </Descriptions>
            </Card>
          </Col>
          <Col xs={24} lg={12}>
            <Card title="Customer" style={{ marginBottom: 16 }}>
              <Form.Item name="fullName" label="Full Name" rules={[{ required: true, message: 'Enter the full name.' }]}>
                <Input disabled={!editAll} maxLength={100} />
              </Form.Item>
              <Form.Item name="gender" label="Gender">
                <Radio.Group
                  disabled={!editAll}
                  options={[
                    { value: 'Male', label: 'Male' },
                    { value: 'Female', label: 'Female' },
                  ]}
                />
              </Form.Item>
              <Form.Item name="email" label="Email" rules={[{ required: true, message: 'Enter the email.' }]}>
                <Input disabled={!editAll} maxLength={100} />
              </Form.Item>
              <Form.Item name="mobile" label="Mobile" rules={[{ required: true, message: 'Enter the mobile number.' }]}>
                <Input disabled={!editAll} maxLength={10} />
              </Form.Item>
              {/* UC-60 A4, UC-61 A1. */}
              {r?.hasAccount && (
                <Alert
                  type="info"
                  showIcon
                  title="This email already has an account: the registration belongs to it, and no account is created when it becomes Paid."
                />
              )}
            </Card>
          </Col>
        </Row>

        <Card title="Notes" style={{ marginBottom: 16 }}>
          <Form.Item name="notes" style={{ margin: 0 }}>
            <Input.TextArea aria-label="Notes" rows={3} maxLength={BRIEF_MAX} showCount placeholder="Remarks of the Sale user (BR-22: can be added at any status)" />
          </Form.Item>
        </Card>

        <Flex gap={8} justify="end">
          <Button onClick={() => navigate(backTo)}>Cancel</Button>
          <Button type="primary" htmlType="submit" loading={save.isPending}>
            Save
          </Button>
        </Flex>
      </Form>
    </>
  )
}

/** The read-only facts of a stored registration: when, by whom, status, validity, invoice. */
function RecordInfo({ r }: { r: RegistrationDetails }) {
  return (
    <Card style={{ marginBottom: 16 }}>
      <Descriptions column={{ xs: 1, md: 3 }} size="small">
        <Descriptions.Item label="Registration Time">{dayjs(r.registeredAt).format('DD/MM/YYYY HH:mm')}</Descriptions.Item>
        <Descriptions.Item label="Sale">{r.recordedBy ?? 'Customer (online)'}</Descriptions.Item>
        <Descriptions.Item label="Last Updated By">{r.updatedBy}</Descriptions.Item>
        <Descriptions.Item label="Status">
          <StatusTag status={r.status} />
        </Descriptions.Item>
        <Descriptions.Item label="Valid From">{day(r.validFrom)}</Descriptions.Item>
        <Descriptions.Item label="Valid To">{day(r.validTo)}</Descriptions.Item>
      </Descriptions>
      {r.invoice && (
        <Alert
          style={{ marginTop: 12 }}
          type={r.invoice.status === 'Unpaid' ? 'warning' : 'info'}
          showIcon
          title={
            <Space wrap>
              <span>
                In invoice <Typography.Text strong>{r.invoice.code}</Typography.Text> ({formatVnd(r.invoice.totalAmount)})
              </span>
              <StatusTag status={r.invoice.status} />
              <Link to={`/manage/invoices/${r.invoice.id}`}>Invoice Details</Link>
            </Space>
          }
          description={
            r.invoice.status === 'Unpaid'
              ? 'It becomes Paid when the invoice is confirmed on Invoice Details; only the notes can change here (BR-24).'
              : undefined
          }
        />
      )}
      {!r.canEditAll && r.status === 'Submitted' && !r.invoice && (
        <Typography.Paragraph type="secondary" style={{ margin: '12px 0 0' }}>
          Recorded by {r.recordedBy ?? 'the customer online'}: only the status and the notes can change (BR-22).
        </Typography.Paragraph>
      )}
    </Card>
  )
}
