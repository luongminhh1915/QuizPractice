import { useState } from 'react'
import { Alert, App, Button, Descriptions, Flex, Modal, Radio, Result, Space, Spin, Typography } from 'antd'
import dayjs from 'dayjs'
import { errorMessage, problemOf } from '../../api/client'
import { useOpenAccountModal } from '../account/accountModalContext'
import { useRegisterForm, useSubmitRegistration } from '../../hooks/usePublic'
import { formatVnd } from '../../utils/money'

/**
 * Subject Register (SRS III 2.6; F#12; UC-14), for a signed-in customer. The contact is
 * read from the profile and never typed here (OI-15); Edit Profile opens User Profile on
 * top, and the pop-up shows the new values once it is saved (E1). Submit and Pay by QR
 * (A2) is left out until UC-18 exists.
 */
export function SubjectRegisterModal({ subjectId, open, onClose }: { subjectId: number; open: boolean; onClose: () => void }) {
  const { message } = App.useApp()
  const openModal = useOpenAccountModal()
  const form = useRegisterForm(subjectId, open)
  const submit = useSubmitRegistration(subjectId)
  const [packageId, setPackageId] = useState<number>()
  const [done, setDone] = useState<{ packageName: string; totalCost: number }>()

  const close = () => {
    setPackageId(undefined)
    setDone(undefined)
    submit.reset()
    onClose()
  }

  const onSubmit = () =>
    packageId &&
    submit.mutate(packageId, {
      onSuccess: (r) => setDone({ packageName: r.packageName, totalCost: r.totalCost }),
      onError: (e) => {
        // E2: the package went away meanwhile; reload the list and choose again.
        if (problemOf(e)?.code === 'Package.NotAvailable') {
          setPackageId(undefined)
          void form.refetch()
        }
        message.error(errorMessage(e))
      },
    })

  const data = form.data
  const chosen = data?.packages.find((p) => p.id === packageId)
  const problems = Object.entries(data?.contactProblems ?? {})

  let body
  if (form.isPending) body = <Spin style={{ display: 'block', padding: 32 }} />
  else if (form.isError) body = <Result status="warning" title={errorMessage(form.error)} />
  else if (done) {
    // Step 7: stored, and how it is settled.
    body = (
      <Result
        status="success"
        title="Registration submitted"
        subTitle={`${data!.subjectName}, ${done.packageName}, ${formatVnd(done.totalCost)}. The Sale team will contact you to settle it; until then you can still change or cancel it in My Registrations.`}
        extra={<Button onClick={close}>Close</Button>}
      />
    )
  } else if (data?.existing) {
    // A3: one Submitted registration per subject.
    body = (
      <Result
        status="info"
        title="You have already registered for this subject"
        subTitle={`${data.existing.packageName}, ${formatVnd(data.existing.totalCost)}, submitted on ${dayjs(data.existing.registeredAt).format('DD/MM/YYYY HH:mm')}. It is waiting for the Sale team and can be changed in My Registrations until it is settled.`}
        extra={<Button onClick={close}>Close</Button>}
      />
    )
  } else if (data) {
    body = (
      <Flex vertical gap={16}>
        {/* E2 and A2: every option is an active package; the cheapest comes first. */}
        <div>
          <Typography.Text strong>Price Package</Typography.Text>
          <Radio.Group value={packageId} onChange={(e) => setPackageId(e.target.value)} style={{ width: '100%', marginTop: 8 }}>
            <Flex vertical gap={8}>
              {data.packages.map((p) => (
                <Radio key={p.id} value={p.id}>
                  <Space size={12} wrap>
                    <span>
                      {p.name}
                      {/* The duration, unless the name already says it ("3 months"). */}
                      {!p.name.includes(String(p.accessMonths)) && ` (${p.accessMonths} ${p.accessMonths === 1 ? 'month' : 'months'})`}
                    </span>
                    {p.listPrice > p.salePrice && (
                      <Typography.Text delete type="secondary">
                        {formatVnd(p.listPrice)}
                      </Typography.Text>
                    )}
                    <Typography.Text strong>{formatVnd(p.salePrice)}</Typography.Text>
                  </Space>
                </Radio>
              ))}
            </Flex>
          </Radio.Group>
        </div>

        <div>
          <Flex justify="space-between" align="center" style={{ marginBottom: 8 }}>
            <Typography.Text strong>Contact (from your profile)</Typography.Text>
            <Button size="small" onClick={() => openModal('profile')}>
              Edit Profile
            </Button>
          </Flex>
          {!!problems.length && (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 8 }}
              title="Your profile is missing details the registration needs. Edit your profile first."
              description={problems.map(([field, msgs]) => `${field}: ${msgs.join(' ')}`).join(' ')}
            />
          )}
          <Descriptions size="small" column={1} bordered>
            <Descriptions.Item label="Full Name">{data.contact.fullName}</Descriptions.Item>
            <Descriptions.Item label="Email">{data.contact.email}</Descriptions.Item>
            <Descriptions.Item label="Mobile">{data.contact.mobile}</Descriptions.Item>
            <Descriptions.Item label="Gender">{data.contact.gender}</Descriptions.Item>
          </Descriptions>
        </div>

        <Flex justify="space-between" align="center">
          <Typography.Text>
            Total Cost: <Typography.Text strong>{chosen ? formatVnd(chosen.salePrice) : '—'}</Typography.Text>
          </Typography.Text>
          <Space>
            <Button onClick={close}>Cancel</Button>
            <Button type="primary" disabled={!chosen || !!problems.length} loading={submit.isPending} onClick={onSubmit}>
              Submit
            </Button>
          </Space>
        </Flex>
      </Flex>
    )
  }

  return (
    <Modal title={data ? `Register: ${data.subjectName}` : 'Register'} open={open} onCancel={close} footer={null} destroyOnHidden width={560}>
      {body}
    </Modal>
  )
}
