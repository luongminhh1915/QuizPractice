import { App, Button, Input, Select, Space, Tag, Typography } from 'antd'
import type { QuestionSummary } from '../../api/questionsApi'
import type { VisibilityStatus } from '../../api/types'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useListParams } from '../../hooks/useListParams'
import { useChangeQuestionStatus, useQuestionFilters, useQuestionList } from '../../hooks/useQuestions'

const toNumber = (value: string | undefined) => (value ? Number(value) : undefined)

/** Groups options under a heading (a topic, a dimension type), keeping the order the API gave. */
function grouped<T extends { id: number; name: string }>(items: T[], groupOf: (item: T) => string) {
  const groups = new Map<string, { value: number; label: string }[]>()
  for (const item of items) {
    const group = groupOf(item)
    if (!groups.has(group)) groups.set(group, [])
    groups.get(group)!.push({ value: item.id, label: item.name })
  }
  return [...groups].map(([label, options]) => ({ label, title: label, options }))
}

/** Questions List (SRS III 7.1; PDF II.7, F#30; UC-52 View Questions List, UC-54 Show or Hide Question). */
export function QuestionsListPage() {
  const { message } = App.useApp()
  const list = useListParams()
  const subjectId = toNumber(list.get('subjectId'))
  const lessonId = toNumber(list.get('lessonId'))
  const levelId = toNumber(list.get('levelId'))
  const status = list.get('status') as VisibilityStatus | undefined
  // Several dimensions travel in the address as one value: ?dimensionIds=3,5
  const dimensionIds = list.get('dimensionIds')?.split(',').map(Number)

  const query = useQuestionList({ ...list.criteria, subjectId, lessonId, dimensionIds, levelId, status })
  const filters = useQuestionFilters(subjectId)
  const choices = filters.data
  const changeStatus = useChangeQuestionStatus()

  const toggle = (question: QuestionSummary) => {
    const next: VisibilityStatus = question.status === 'Shown' ? 'Hidden' : 'Shown'
    changeStatus.mutate(
      { id: question.id, status: next },
      {
        // UC-54 A1: a question already in that state (another user got there first) is
        // left as it is; the list reloads either way. A hidden question is no longer
        // drawn into new practices and quizzes.
        onSuccess: () => message.success(`Question #${question.id} is now ${next.toLowerCase()}.`),
        onError: (e) => message.error(errorMessage(e)),
      },
    )
  }

  const columns: DataTableColumn<QuestionSummary>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 80 },
    {
      title: 'Content',
      key: 'content',
      render: (q) => (
        <Typography.Paragraph ellipsis={{ rows: 2, tooltip: q.content }} style={{ margin: 0 }}>
          {q.content}
        </Typography.Paragraph>
      ),
    },
    { title: 'Subject', key: 'subject', sortable: true, width: 170 },
    {
      title: 'Dimension',
      key: 'dimensions',
      width: 190,
      render: (q) => (
        <Space size={[0, 4]} wrap>
          {q.dimensions.map((d) => (
            <Tag key={d}>{d}</Tag>
          ))}
        </Space>
      ),
    },
    {
      title: 'Lesson',
      key: 'lesson',
      width: 160,
      render: (q) => q.lesson ?? <Typography.Text type="secondary">—</Typography.Text>,
    },
    { title: 'Level', key: 'level', sortable: true, width: 100 },
    { title: 'Status', key: 'status', sortable: true, width: 110, render: (q) => <StatusTag status={q.status} /> },
    {
      title: 'Action',
      key: 'action',
      width: 90,
      render: (q) => (
        <Button
          size="small"
          danger={q.status === 'Shown'}
          loading={changeStatus.isPending && changeStatus.variables?.id === q.id}
          onClick={() => toggle(q)}
        >
          {q.status === 'Shown' ? 'Hide' : 'Show'}
        </Button>
      ),
    },
  ]

  const error = query.error ?? filters.error

  return (
    <>
      <PageHeader title="Questions List" />

      <Space wrap style={{ marginBottom: 16 }}>
        {/* key resets the box when the criteria are cleared */}
        <Input.Search
          key={list.criteria.search ?? ''}
          defaultValue={list.criteria.search}
          placeholder="Search content"
          allowClear
          onSearch={(value) => list.set({ search: value.trim() })}
          style={{ width: 260 }}
        />
        <Select
          aria-label="Subject"
          value={subjectId ?? 'All'}
          // The lessons and dimensions belong to the subject, so a new subject clears them.
          onChange={(value) =>
            list.set({ subjectId: value === 'All' ? undefined : value, lessonId: undefined, dimensionIds: undefined })
          }
          loading={filters.isPending}
          options={[
            { value: 'All', label: 'All subjects' },
            ...(choices?.subjects ?? []).map((s) => ({ value: s.id, label: s.name })),
          ]}
          popupMatchSelectWidth={false}
          style={{ width: 220 }}
        />
        <Select
          aria-label="Lesson"
          value={lessonId ?? 'All'}
          disabled={subjectId === undefined}
          onChange={(value) => list.set({ lessonId: value === 'All' ? undefined : value })}
          options={[{ value: 'All', label: 'All lessons' }, ...grouped(choices?.lessons ?? [], (l) => l.topic)]}
          popupMatchSelectWidth={false}
          style={{ width: 190 }}
        />
        <Select
          aria-label="Dimension"
          mode="multiple"
          value={dimensionIds ?? []}
          disabled={subjectId === undefined}
          placeholder="All dimensions"
          allowClear
          maxTagCount="responsive"
          onChange={(values: number[]) => list.set({ dimensionIds: values.length ? values.join(',') : undefined })}
          options={grouped(choices?.dimensions ?? [], (d) => d.type)}
          popupMatchSelectWidth={false}
          style={{ width: 220 }}
        />
        <Select
          aria-label="Level"
          value={levelId ?? 'All'}
          onChange={(value) => list.set({ levelId: value === 'All' ? undefined : value })}
          options={[
            { value: 'All', label: 'All levels' },
            ...(choices?.levels ?? []).map((l) => ({ value: l.id, label: l.name })),
          ]}
          style={{ width: 130 }}
        />
        <Select
          aria-label="Status"
          value={status ?? 'All'}
          onChange={(value) => list.set({ status: value === 'All' ? undefined : value })}
          options={[{ value: 'All', label: 'All statuses' }, { value: 'Shown' }, { value: 'Hidden' }]}
          style={{ width: 140 }}
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
