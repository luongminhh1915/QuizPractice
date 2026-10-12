import { useState } from 'react'
import { Alert, App, Button, Descriptions, Flex, Modal, Radio, Result, Space, Spin, Typography } from 'antd'
import dayjs from 'dayjs'
import { useNavigate } from 'react-router'
import { errorMessage, problemOf } from '../../api/client'
import { useOpenAccountModal } from '../account/accountModalContext'
import { useChangeForm, useChangeRegistration, useCreateInvoice } from '../../hooks/useCustomer'
import { useRegisterForm, useSubmitRegistration } from '../../hooks/usePublic'
import { formatVnd } from '../../utils/money'

/** A new registration of a subject (UC-14), or a change to a stored one (UC-16). */
export type RegisterTarget = { subjectId: number; registrationId?: undefined } | { registrationId: number; subjectId?: undefined }

/**
 * Subject Register (SRS III 2.6; F#12; UC-14, UC-16), for a signed-in customer. The
 * contact is read from the profile and never typed here (OI-15); Edit Profile opens
 * User Profile on top, and the pop-up shows the new values once it is saved (UC-14 E1,
 * UC-16 E2). Opened on a stored registration, only the package can change. Submit and
 * Pay by QR stores the registration and opens the QR code of an invoice for it
 * (UC-14 A2, UC-18 A1).
 */
export function SubjectRegisterModal({ target, open, onClose }: { target: RegisterTarget; open: boolean; onClose: () => void }) {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const openModal = useOpenAccountModal()
  const pay = useCreateInvoice()
  const editing = target.registrationId !== undefined
  const newForm = useRegisterForm(target.subjectId ?? 0, open && !editing)
  const changeForm = useChangeForm(target.registrationId ?? 0, open && editing)
  const submit = useSubmitRegistration(target.subjectId ?? 0)
  const change = useChangeRegistration(target.registrationId ?? 0)
  const form = editing ? changeForm : newForm
  const saving = editing ? change : submit

  const [picked, setPicked] = useState<number>()
  const [done, setDone] = useState<{ packageName: string; totalCost: number }>()

  const close = () => {
    setPicked(undefined)
    setDone(undefined)
    saving.reset()
    onClose()
  }

  const data = form.data
  // Until the customer picks, a reopened registration shows its stored package (UC-16 step 2).
  const packageId = picked ?? data?.selectedPackageId ?? undefined
  const chosen = data?.packages.find((p) => p.id === packageId)
  const problems = Object.entries(data?.contactProblems ?? {})

  const onError = (e: unknown) => {
    const code = problemOf(e)?.code
    if (code === 'Package.NotAvailable') {
      // The package went away meanwhile; reload the list and choose again (UC-14 E2).
      setPicked(undefined)
      void form.refetch()
      message.error(errorMessage(e))
    } else if (code === 'Registration.Settled' || code === 'Registration.InUnpaidInvoice') {
      // UC-16 E1, E3: nothing to change here any more; the list reloads with the reason.
      message.warning(errorMessage(e))
      close()
    } else {
      message.error(errorMessage(e))
    }
  }

  // UC-14 A2: store, then one invoice for this registration and its QR code. If the
  // invoice fails (e.g. no receiving account, UC-18 E2), the registration stays stored.
  const onSubmitAndPay = () => {
    if (!packageId) return
    submit.mutate(packageId, {
      onError,
      onSuccess: (r) =>
        pay.mutate([r.id], {
          onSuccess: (invoice) => {
            close()
            navigate(`/my-invoices/${invoice.id}`)
          },
          onError: (e) => {
            setDone({ packageName: r.packageName, totalCost: r.totalCost })
            message.warning(`Your registration was submitted, but the QR code could not be made: ${errorMessage(e)}`)
          },
        }),
    })
  }

  const onSave = () => {
    if (!packageId) return
    const onSuccess = (r: { packageName: string; totalCost: number }) => setDone({ packageName: r.packageName, totalCost: r.totalCost })
    if (editing) change.mutate(packageId, { onSuccess, onError })
    else submit.mutate(packageId, { onSuccess, onError })
  }

  let body
  if (form.isPending) body = <Spin style={{ display: 'block', padding: 32 }} />
  else if (form.isError) body = <Result status="warning" title={errorMessage(form.error)} />
  else if (done) {
    // UC-14 step 7, UC-16 step 5: stored, and how it is settled.
    body = (
      <Result
        status="success"
        title={editing ? 'Registration changed' : 'Registration submitted'}
        subTitle={`${data!.subjectName}, ${done.packageName}, ${formatVnd(done.totalCost)}. The Sale team will contact you to settle it; until then you can still change or cancel it in My Registrations.`}
        extra={<Button onClick={close}>Close</Button>}
      />
    )
  } else if (data?.existing) {
    // UC-14 A3: one Submitted registration per subject.
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
        <div>
          <Typography.Text strong>Price Package</Typography.Text>
          <Radio.Group value={packageId} onChange={(e) => setPicked(e.target.value)} style={{ width: '100%', marginTop: 8 }}>
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
            <Button type={editing ? 'primary' : 'default'} disabled={!chosen || !!problems.length} loading={saving.isPending && !pay.isPending} onClick={onSave}>
              {editing ? 'Save' : 'Submit'}
            </Button>
            {!editing && (
              <Button type="primary" disabled={!chosen || !!problems.length} loading={pay.isPending} onClick={onSubmitAndPay}>
                Submit and Pay by QR
              </Button>
            )}
          </Space>
        </Flex>
      </Flex>
    )
  }

  const title = data ? `${editing ? 'Change registration' : 'Register'}: ${data.subjectName}` : editing ? 'Change registration' : 'Register'
  return (
    <Modal title={title} open={open} onCancel={close} footer={null} destroyOnHidden width={560}>
      {body}
    </Modal>
  )
}
