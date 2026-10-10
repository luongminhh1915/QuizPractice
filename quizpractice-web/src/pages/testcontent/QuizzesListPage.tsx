import { App, Button, Input, Popconfirm, Select, Space, Tag, Tooltip, Typography } from 'antd'
import type { QuizSummary } from '../../api/quizzesApi'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { useListParams } from '../../hooks/useListParams'
import { useDeleteQuiz, useQuizFilters, useQuizList } from '../../hooks/useQuizzes'

const toNumber = (value: string | undefined) => (value ? Number(value) : undefined)

/** Quizzes List (SRS III 7.4; PDF II.7, F#33; UC-56 View Quizzes List, UC-58 Delete Quiz). */
export function QuizzesListPage() {
  const { message } = App.useApp()
  const list = useListParams()
  const subjectId = toNumber(list.get('subjectId'))
  const typeId = toNumber(list.get('typeId'))

  const query = useQuizList({ ...list.criteria, subjectId, typeId })
  const filters = useQuizFilters()
  const remove = useDeleteQuiz()

  const deleteQuiz = (quiz: QuizSummary) =>
    remove.mutate(quiz.id, {
      onSuccess: () => message.success(`Quiz "${quiz.name}" was deleted.`),
      // E2, E3: the API says why the quiz stays (taken, or the lesson that opens it).
      onError: (e) => message.error(errorMessage(e), 8),
    })

  const columns: DataTableColumn<QuizSummary>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 80 },
    {
      title: 'Name',
      key: 'name',
      render: (q) => (
        <Space size={6} wrap>
          <span>{q.name}</span>
          {q.isTaken && (
            <Tooltip title="A customer has taken it: it can be viewed, no longer edited or deleted (BR-13).">
              <Tag color="blue">Taken</Tag>
            </Tooltip>
          )}
          {q.status === 'Inactive' && (
            <Tooltip title="Not offered to customers.">
              <Tag>Inactive</Tag>
            </Tooltip>
          )}
        </Space>
      ),
    },
    { title: 'Subject', key: 'subject', sortable: true, width: 220 },
    { title: 'Level', key: 'level', sortable: true, width: 100 },
    { title: 'Questions', key: 'questionCount', width: 100 },
    { title: 'Duration', key: 'durationMin', width: 100, render: (q) => `${q.durationMin} min` },
    { title: 'Pass Rate', key: 'passRate', width: 100, render: (q) => `${q.passRate}%` },
    {
      title: 'Type',
      key: 'type',
      width: 120,
      render: (q) => <Tag color={q.type === 'Simulation' ? 'purple' : 'cyan'}>{q.type}</Tag>,
    },
    {
      title: 'Action',
      key: 'action',
      width: 100,
      // A taken quiz keeps its results: no Delete (BR-13, UC-58 E2).
      render: (q) =>
        q.isTaken ? null : (
          <Popconfirm
            title="Delete this quiz?"
            description={`"${q.name}" and its question rules are removed for good.`}
            okText="Delete"
            okButtonProps={{ danger: true }}
            cancelText="Cancel"
            onConfirm={() => deleteQuiz(q)}
          >
            <Button size="small" danger loading={remove.isPending && remove.variables === q.id}>
              Delete
            </Button>
          </Popconfirm>
        ),
    },
  ]

  const error = query.error ?? filters.error

  return (
    <>
      <PageHeader title="Quizzes List" />

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
          aria-label="Subject"
          value={subjectId ?? 'All'}
          onChange={(value) => list.set({ subjectId: value === 'All' ? undefined : value })}
          loading={filters.isPending}
          options={[
            { value: 'All', label: 'All subjects' },
            ...(filters.data?.subjects ?? []).map((s) => ({ value: s.id, label: s.name })),
          ]}
          popupMatchSelectWidth={false}
          style={{ width: 240 }}
        />
        <Select
          aria-label="Quiz Type"
          value={typeId ?? 'All'}
          onChange={(value) => list.set({ typeId: value === 'All' ? undefined : value })}
          options={[
            { value: 'All', label: 'All types' },
            ...(filters.data?.types ?? []).map((t) => ({ value: t.id, label: t.name })),
          ]}
          style={{ width: 150 }}
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
