import { Button, Input, Select, Space, Typography } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import { Link, useLocation } from 'react-router'
import type { SubjectStatus, SubjectSummary } from '../../api/subjectsApi'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useCurrentUser } from '../../hooks/useAccount'
import { useListParams } from '../../hooks/useListParams'
import { useSubjectFilters, useSubjectList } from '../../hooks/useSubjects'

const toNumber = (value: string | undefined) => (value ? Number(value) : undefined)

const STATUSES: SubjectStatus[] = ['Published', 'Unpublished']

/** Subject Management List (SRS III 6.1; PDF II.6, F#23; UC-41). An Expert only sees the assigned subjects (BR-11). */
export function SubjectManagementListPage() {
  const location = useLocation()
  const isAdmin = useCurrentUser().data?.role === 'Admin'
  const list = useListParams()
  const categoryId = toNumber(list.get('categoryId'))
  const status = list.get('status') as SubjectStatus | undefined

  const query = useSubjectList({ ...list.criteria, categoryId, status })
  const filters = useSubjectFilters()

  const columns: DataTableColumn<SubjectSummary>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 80 },
    { title: 'Name', key: 'name', sortable: true },
    { title: 'Category', key: 'category', sortable: true, width: 160 },
    { title: 'Lessons', key: 'lessons', sortable: true, width: 100, render: (s) => s.lessonCount },
    {
      title: 'Owner',
      key: 'owner',
      sortable: true,
      width: 200,
      render: (s) => s.owner ?? <Typography.Text type="secondary">Not assigned</Typography.Text>,
    },
    { title: 'Status', key: 'status', sortable: true, width: 120, render: (s) => <StatusTag status={s.status} /> },
  ]

  const error = query.error ?? filters.error

  // The detail screens return to this list with the same criteria (SRS III 0.3).
  const back = { state: { back: location.pathname + location.search } }

  return (
    <>
      <PageHeader
        title="Subject Management List"
        action={
          // Only the Admin creates a subject (F#24); an Expert never sees the action (A4).
          isAdmin && (
            <Link to="/manage/subjects/new" {...back}>
              <Button type="primary" icon={<PlusOutlined />}>
                Add New Subject
              </Button>
            </Link>
          )
        }
      />

      <Space wrap style={{ marginBottom: 16 }}>
        {/* key resets the box when the criteria are cleared */}
        <Input.Search
          key={list.criteria.search ?? ''}
          defaultValue={list.criteria.search}
          placeholder="Search name"
          allowClear
          onSearch={(value) => list.set({ search: value.trim() })}
          style={{ width: 260 }}
        />
        <Select
          aria-label="Category"
          value={categoryId ?? 'All'}
          onChange={(value) => list.set({ categoryId: value === 'All' ? undefined : value })}
          loading={filters.isPending}
          options={[
            { value: 'All', label: 'All categories' },
            ...(filters.data?.categories ?? []).map((c) => ({ value: c.id, label: c.name })),
          ]}
          popupMatchSelectWidth={false}
          style={{ width: 200 }}
        />
        <Select
          aria-label="Status"
          value={status ?? 'All'}
          onChange={(value) => list.set({ status: value === 'All' ? undefined : value })}
          options={[{ value: 'All', label: 'All statuses' }, ...STATUSES.map((s) => ({ value: s, label: s }))]}
          style={{ width: 160 }}
        />
      </Space>

      {error && <Typography.Paragraph type="danger">{errorMessage(error)}</Typography.Paragraph>}

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
