import { App, Button, Input, Select, Space, Tag, Typography } from 'antd'
import { Link, useLocation, useParams } from 'react-router'
import type { LessonRow } from '../../api/subjectsApi'
import type { ActiveStatus } from '../../api/types'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useListParams } from '../../hooks/useListParams'
import { useChangeLessonStatus, useLessons } from '../../hooks/useSubjects'

const toNumber = (value: string | undefined) => (value ? Number(value) : undefined)

const TYPE_COLOURS: Record<string, string> = { 'Subject Topic': 'geekblue', Lesson: 'default', Quiz: 'purple' }

/**
 * Subject Lessons (SRS III 6.6; PDF II.6, F#28; UC-49 View Subject Lessons, UC-51 Activate or
 * Deactivate Lesson). The topics in order, each followed by its lessons and quizzes, indented.
 */
export function SubjectLessonsPage() {
  const { message } = App.useApp()
  const location = useLocation()
  const subjectId = Number(useParams().id)
  const list = useListParams()
  const topicId = toNumber(list.get('topicId'))
  const status = list.get('status') as ActiveStatus | undefined

  const query = useLessons(subjectId, { page: list.criteria.page, pageSize: list.criteria.pageSize, search: list.criteria.search, topicId, status })
  const changeStatus = useChangeLessonStatus(subjectId)
  const data = query.data

  const toggle = (lesson: LessonRow) => {
    const next = lesson.status === 'Active' ? 'Inactive' : 'Active'
    changeStatus.mutate(
      { id: lesson.id, status: next },
      {
        onSuccess: () => message.success(`"${lesson.name}" is now ${next}.`),
        onError: (e) => message.error(errorMessage(e)),
      },
    )
  }

  const columns: DataTableColumn<LessonRow>[] = [
    { title: 'ID', key: 'id', width: 80 },
    {
      title: 'Name',
      key: 'name',
      // A lesson or a quiz sits under its topic (SRS III 6.6).
      render: (l) => (l.isTopic ? <Typography.Text strong>{l.name}</Typography.Text> : <span style={{ paddingLeft: 28 }}>{l.name}</span>),
    },
    { title: 'Order', key: 'displayOrder', width: 80 },
    { title: 'Type', key: 'type', width: 140, render: (l) => <Tag color={TYPE_COLOURS[l.type]}>{l.type}</Tag> },
    { title: 'Status', key: 'status', width: 110, render: (l) => <StatusTag status={l.status} /> },
    {
      title: 'Action',
      key: 'action',
      width: 130,
      render: (l) => (
        // UC-51: the label names the action available.
        <Button
          size="small"
          danger={l.status === 'Active'}
          loading={changeStatus.isPending && changeStatus.variables?.id === l.id}
          onClick={() => toggle(l)}
        >
          {l.status === 'Active' ? 'Deactivate' : 'Activate'}
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader title="Subject Lessons" />
      {data && (
        <Typography.Paragraph>
          {/* The lessons belong to the subject, not to a price package (OI-02). */}
          Subject:{' '}
          <Link to={`/manage/subjects/${subjectId}`} state={location.state}>
            {data.subject}
          </Link>
        </Typography.Paragraph>
      )}

      <Space wrap style={{ marginBottom: 16 }}>
        <Input.Search
          key={list.criteria.search ?? ''}
          defaultValue={list.criteria.search}
          placeholder="Search name"
          allowClear
          onSearch={(value) => list.set({ search: value.trim() })}
          style={{ width: 260 }}
        />
        <Select
          aria-label="Topic"
          value={topicId ?? 'All'}
          onChange={(value) => list.set({ topicId: value === 'All' ? undefined : value })}
          options={[{ value: 'All', label: 'All topics' }, ...(data?.topics ?? []).map((t) => ({ value: t.id, label: t.name }))]}
          popupMatchSelectWidth={false}
          style={{ width: 260 }}
        />
        <Select
          aria-label="Lesson Status"
          value={status ?? 'All'}
          onChange={(value) => list.set({ status: value === 'All' ? undefined : value })}
          options={[
            { value: 'All', label: 'All statuses' },
            { value: 'Active', label: 'Active' },
            { value: 'Inactive', label: 'Inactive' },
          ]}
          style={{ width: 150 }}
        />
      </Space>

      {query.error && <Typography.Paragraph type="danger">{errorMessage(query.error)}</Typography.Paragraph>}

      <DataTable
        columns={columns}
        page={data?.lessons}
        loading={query.isFetching}
        criteria={list.criteria}
        onChange={list.set}
        hasCriteria={list.hasCriteria}
        onClearCriteria={list.clear}
      />
    </>
  )
}
