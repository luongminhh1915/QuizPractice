import { Button, DatePicker, Flex, Input, Select, Space, Tooltip, Typography } from 'antd'
import { PlusOutlined, SortAscendingOutlined, SortDescendingOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { Link, useLocation } from 'react-router'
import type { RegistrationStatus } from '../../api/customerApi'
import type { RegistrationSummary } from '../../api/saleApi'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useListParams } from '../../hooks/useListParams'
import { useRegistrationFilters, useRegistrationList } from '../../hooks/useSale'
import { formatVnd } from '../../utils/money'

/** The nine sortable columns of SRS III 8.1, in one control because the table has no room for nine sort headings. */
const SORT_OPTIONS = [
  { value: 'registeredAt', label: 'Registration time' },
  { value: 'id', label: 'ID' },
  { value: 'email', label: 'Email' },
  { value: 'subject', label: 'Subject' },
  { value: 'package', label: 'Package' },
  { value: 'totalCost', label: 'Total cost' },
  { value: 'status', label: 'Status' },
  { value: 'validFrom', label: 'Valid from' },
  { value: 'validTo', label: 'Valid to' },
]

const day = (value: string | null) => (value ? dayjs(value).format('DD/MM/YYYY') : '—')

/** Registrations List (SRS III 8.1; F#35; UC-59): every registration, for the Sale role. */
export function RegistrationsListPage() {
  const location = useLocation()
  const list = useListParams()
  // Registration Details returns to this list with the same criteria (SRS III 0.3).
  const back = { state: { back: location.pathname + location.search } }
  const subjectId = list.get('subjectId') ? Number(list.get('subjectId')) : undefined
  const status = list.get('status') as RegistrationStatus | undefined
  const from = list.get('from')
  const to = list.get('to')
  // No sort chosen = the newest registration first.
  const sortBy = list.criteria.sortBy ?? 'registeredAt'
  const descending = list.criteria.sortBy ? Boolean(list.criteria.descending) : true

  const query = useRegistrationList({ ...list.criteria, subjectId, status, from, to })
  const filters = useRegistrationFilters()

  const columns: DataTableColumn<RegistrationSummary>[] = [
    { title: 'ID', key: 'id', width: 70 },
    {
      title: 'Email',
      key: 'email',
      render: (r) => (
        <Flex vertical>
          <span>{r.email}</span>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {dayjs(r.registeredAt).format('DD/MM/YYYY HH:mm')}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: 'Subject',
      key: 'subject',
      render: (r) => (
        <Flex vertical>
          <span>{r.subjectName}</span>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {r.packageName}
          </Typography.Text>
        </Flex>
      ),
    },
    { title: 'Total Cost', key: 'totalCost', width: 120, render: (r) => formatVnd(r.totalCost) },
    { title: 'Status', key: 'status', width: 110, render: (r) => <StatusTag status={r.status} /> },
    {
      title: 'Valid',
      key: 'valid',
      width: 130,
      render: (r) => (
        <Flex vertical style={{ fontSize: 12 }}>
          <span>From {day(r.validFrom)}</span>
          <span>To {day(r.validTo)}</span>
        </Flex>
      ),
    },
    { title: 'Last Updated By', key: 'updatedBy', width: 200 },
    {
      title: 'Action',
      key: 'action',
      width: 80,
      render: (r) => (
        <Link to={`/manage/registrations/${r.id}`} {...back}>
          <Button size="small">Edit</Button>
        </Link>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Registrations List"
        action={
          <Link to="/manage/registrations/new" {...back}>
            <Button type="primary" icon={<PlusOutlined />}>
              Add New Registration
            </Button>
          </Link>
        }
      />

      <Space wrap style={{ marginBottom: 16 }}>
        <Input.Search
          key={list.criteria.search ?? ''}
          defaultValue={list.criteria.search}
          placeholder="Search email"
          allowClear
          onSearch={(value) => list.set({ search: value.trim() })}
          style={{ width: 240 }}
        />
        {/* Typing narrows the subjects, as F#35 asks for a searchable filter. */}
        <Select
          aria-label="Subject"
          showSearch
          optionFilterProp="label"
          value={subjectId ?? 'All'}
          onChange={(value) => list.set({ subjectId: value === 'All' ? undefined : value })}
          loading={filters.isPending}
          options={[
            { value: 'All', label: 'All subjects' },
            ...(filters.data?.subjects ?? []).map((s) => ({ value: s.id, label: s.name })),
          ]}
          popupMatchSelectWidth={false}
          style={{ width: 220 }}
        />
        <DatePicker.RangePicker
          aria-label="Registration Time"
          format="DD/MM/YYYY"
          value={from && to ? [dayjs(from), dayjs(to)] : null}
          onChange={(range) =>
            list.set({ from: range?.[0]?.format('YYYY-MM-DD'), to: range?.[1]?.format('YYYY-MM-DD') })
          }
          placeholder={['Registered from', 'to']}
        />
        <Select
          aria-label="Status"
          value={status ?? 'All'}
          onChange={(value) => list.set({ status: value === 'All' ? undefined : value })}
          options={['All', 'Submitted', 'Paid', 'Cancelled'].map((v) => ({ value: v, label: v === 'All' ? 'All statuses' : v }))}
          style={{ width: 140 }}
        />
        <Space.Compact>
          <Select
            aria-label="Sort By"
            value={sortBy}
            onChange={(value) => list.set({ sortBy: value, descending: value === 'registeredAt' ? true : undefined })}
            options={SORT_OPTIONS}
            style={{ width: 170 }}
          />
          <Tooltip title={descending ? 'Descending' : 'Ascending'}>
            <Button
              aria-label="Sort direction"
              icon={descending ? <SortDescendingOutlined /> : <SortAscendingOutlined />}
              onClick={() => list.set({ sortBy, descending: !descending || undefined })}
            />
          </Tooltip>
        </Space.Compact>
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
