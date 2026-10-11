import { useState } from 'react'
import { App, Button, Card, Col, DatePicker, Empty, Flex, Row, Space, Spin, Statistic, Typography } from 'antd'
import { Link, useSearchParams } from 'react-router'
import dayjs, { type Dayjs } from 'dayjs'
import { Bar, BarChart, CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { errorMessage } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { useDashboard } from '../../hooks/useDashboard'

const DAY = 'YYYY-MM-DD'
const vnd = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 })

/** A figure card; a link only when the role may open the screen behind it (SRS III 5.1, OI-10). */
function FigureCard({ title, to, children }: { title: string; to: string | null; children: React.ReactNode }) {
  const card = (
    <Card title={title} hoverable={to !== null} style={{ height: '100%' }}>
      {children}
    </Card>
  )
  return to ? (
    <Link to={to} aria-label={`Open the screen behind ${title}`}>
      {card}
    </Link>
  ) : (
    card
  )
}

/**
 * Management Dashboard (SRS III 5.1; PDF II.5, F#19; UC-34). The range sits in the
 * address (?from=…&to=…); without it the API uses the last seven days.
 */
export function DashboardPage() {
  const { message } = App.useApp()
  const [params, setParams] = useSearchParams()
  const range = { from: params.get('from') ?? undefined, to: params.get('to') ?? undefined }
  const query = useDashboard(range)
  const data = query.data

  // The pickers hold a draft; Apply puts it in the address. Until then they show the range in use.
  const [draft, setDraft] = useState<{ from?: Dayjs | null; to?: Dayjs | null }>({})
  const from = draft.from !== undefined ? draft.from : data ? dayjs(data.from) : null
  const to = draft.to !== undefined ? draft.to : data ? dayjs(data.to) : null

  const apply = () => {
    if (!from || !to) {
      message.error('Choose both the start date and the end date.')
      return
    }

    // E1: the range in use stays as it is.
    if (to.isBefore(from, 'day')) {
      message.error('The end date is earlier than the start date. The previous range is kept.')
      return
    }

    setDraft({})
    setParams({ from: from.format(DAY), to: to.format(DAY) })
  }

  // A2: back to the last seven days.
  const lastSevenDays = () => {
    setDraft({})
    setParams({})
  }

  if (query.isPending) return <Spin />
  if (!data) return <Typography.Paragraph type="danger">{errorMessage(query.error)}</Typography.Paragraph>

  const registrations = data.paidRegistrations + data.submittedRegistrations + data.cancelledRegistrations
  const noOrder = data.trend.every((p) => p.all === 0)
  const trend = data.trend.map((p) => ({ ...p, day: dayjs(p.date).format('DD/MM') }))

  return (
    <>
      <PageHeader title="Dashboard" />

      <Flex gap={8} wrap align="center" style={{ marginBottom: 16 }}>
        <DatePicker aria-label="Start Date" value={from} onChange={(d) => setDraft((r) => ({ ...r, from: d }))} format="DD/MM/YYYY" allowClear={false} />
        <span>to</span>
        <DatePicker aria-label="End Date" value={to} onChange={(d) => setDraft((r) => ({ ...r, to: d }))} format="DD/MM/YYYY" allowClear={false} />
        <Button type="primary" onClick={apply} loading={query.isFetching}>
          Apply
        </Button>
        <Button onClick={lastSevenDays}>Last 7 Days</Button>
        {query.isError && <Typography.Text type="danger">{errorMessage(query.error)}</Typography.Text>}
      </Flex>

      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} xl={6}>
          <FigureCard title="Subjects" to={data.links.subjects}>
            <Space size="large">
              <Statistic title="New" value={data.newSubjects} />
              <Statistic title="Total" value={data.totalSubjects} />
            </Space>
          </FigureCard>
        </Col>
        <Col xs={24} sm={12} xl={6}>
          <FigureCard title="Registrations" to={data.links.registrations}>
            <Space size="large">
              <Statistic title="Paid" value={data.paidRegistrations} styles={{ content: { color: '#389e0d' } }} />
              <Statistic title="Submitted" value={data.submittedRegistrations} />
              <Statistic title="Cancelled" value={data.cancelledRegistrations} styles={{ content: { color: '#cf1322' } }} />
            </Space>
            <Typography.Text type="secondary">{registrations} made in the range</Typography.Text>
          </FigureCard>
        </Col>
        <Col xs={24} sm={12} xl={6}>
          <FigureCard title="Revenue" to={data.links.revenue}>
            <Statistic title="Became Paid in the range" value={vnd.format(data.revenue)} />
          </FigureCard>
        </Col>
        <Col xs={24} sm={12} xl={6}>
          <FigureCard title="Customers" to={data.links.customers}>
            <Space size="large">
              <Statistic title="Registered" value={data.newCustomers} />
              <Statistic title="Bought" value={data.newBuyers} />
            </Space>
          </FigureCard>
        </Col>

        <Col xs={24} xl={14}>
          <Card title="Order Trend">
            {noOrder ? (
              // A1: no order in the range; the other figures stay.
              <Empty description="No order in this range." style={{ padding: '48px 0' }} />
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <LineChart data={trend} margin={{ top: 8, right: 28, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  {/* Counts: straight segments, no curve dipping between whole numbers. */}
                  <XAxis dataKey="day" minTickGap={12} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Legend />
                  {/* No animation: moving from one range to the next would show counts between whole numbers. */}
                  <Line type="linear" dataKey="all" name="All orders" stroke="#1677ff" strokeWidth={2} isAnimationActive={false} />
                  <Line type="linear" dataKey="paid" name="Paid orders" stroke="#389e0d" strokeWidth={2} isAnimationActive={false} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </Card>
        </Col>
        <Col xs={24} xl={10}>
          <Card title="Revenue by Category">
            <ResponsiveContainer width="100%" height={280}>
              <BarChart data={data.revenueByCategory} margin={{ top: 8, right: 16, left: 16, bottom: 8 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="category" interval={0} angle={-30} textAnchor="end" height={60} tick={{ fontSize: 12 }} />
                <YAxis tickFormatter={(v: number) => `${v / 1_000_000}M`} />
                <Tooltip formatter={(v) => vnd.format(Number(v))} />
                <Bar dataKey="revenue" name="Revenue" fill="#722ed1" isAnimationActive={false} />
              </BarChart>
            </ResponsiveContainer>
          </Card>
        </Col>
      </Row>
    </>
  )
}
