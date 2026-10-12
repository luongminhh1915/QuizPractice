import { Button, Empty, Flex, Result, Select, Space, Table, Tag, Typography, type TableColumnsType } from 'antd'
import dayjs from 'dayjs'
import { useNavigate } from 'react-router'
import type { PracticeSummary } from '../../api/practiceApi'
import { errorMessage } from '../../api/client'
import { PAGE_SIZE, useListParams } from '../../hooks/useListParams'
import { usePracticeFilters, usePractices } from '../../hooks/usePractices'
import { formatDuration } from '../../utils/duration'

type Mode = 'Practice' | 'Exam'

/**
 * Practices List (SRS III 3.2; F#14; UC-19): every practice and simulation exam the
 * customer has taken, the newest first, filtered by subject or type. New Practice and
 * View Details open Practice Details (UC-20, UC-21); an attempt still running continues
 * on Quiz Handle.
 */
export function PracticesListPage() {
  const navigate = useNavigate()
  const { criteria, get, set } = useListParams()
  const subjectId = get('subjectId') ? Number(get('subjectId')) : undefined
  const mode = get('mode') as Mode | undefined
  const oldestFirst = get('oldestFirst') === 'true'
  const list = usePractices({ subjectId, mode, oldestFirst: oldestFirst || undefined, page: criteria.page, pageSize: PAGE_SIZE })
  const filters = usePracticeFilters()
  const filtered = subjectId !== undefined || mode !== undefined

  const columns: TableColumnsType<PracticeSummary> = [
    {
      title: 'Practice',
      // The subject and, under it, the exam name or the practice criteria.
      render: (_, p) => (
        <Flex vertical>
          <Typography.Text strong>{p.subjectName}</Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {p.mode === 'Exam' ? <Tag color="purple">Exam</Tag> : null}
            {p.title}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: 'Date Taken',
      key: 'date',
      sorter: true,
      sortOrder: oldestFirst ? 'ascend' : 'descend',
      sortDirections: ['descend', 'ascend', 'descend'],
      render: (_, p) => dayjs(p.startedAt).format('DD/MM/YYYY HH:mm'),
    },
    {
      title: 'Result',
      align: 'right',
      render: (_, p) =>
        p.outcome ? (
          <span>
            {p.outcome.correct} / {p.questionCount} ({p.outcome.score}%)
          </span>
        ) : (
          <Tag color="blue">In progress</Tag>
        ),
    },
    {
      title: 'Outcome',
      // BR-21: against the pass rate stored with the attempt.
      render: (_, p) =>
        p.outcome ? <Tag color={p.outcome.passed ? 'green' : 'red'}>{p.outcome.passed ? 'Passed' : 'Failed'}</Tag> : '—',
    },
    { title: 'Duration', render: (_, p) => (p.outcome ? formatDuration(p.outcome.durationSeconds * 1000) : '—') },
    {
      title: 'Action',
      render: (_, p) =>
        p.outcome ? (
          <Button size="small" onClick={() => navigate(`/practices/${p.id}`)}>
            View Details
          </Button>
        ) : (
          <Button size="small" type="primary" ghost onClick={() => navigate(`/quiz/${p.id}`)}>
            Continue
          </Button>
        ),
    },
  ]

  return (
    <Flex vertical gap={16}>
      <Flex justify="space-between" align="center" wrap gap={8}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Practices List
        </Typography.Title>
        <Space wrap>
          <Button onClick={() => navigate('/simulation-exams')}>Simulation Exams</Button>
          <Button type="primary" onClick={() => navigate('/practices/new')}>
            New Practice
          </Button>
        </Space>
      </Flex>
      <Space wrap>
        <Select<number | 'All'>
          style={{ width: 280 }}
          value={subjectId ?? 'All'}
          onChange={(v) => set({ subjectId: v === 'All' ? undefined : v })}
          options={[
            { value: 'All' as const, label: 'All subjects' },
            ...(filters.data?.subjects ?? []).map((s) => ({ value: s.id, label: s.name })),
          ]}
        />
        <Select<Mode | 'All'>
          style={{ width: 180 }}
          value={mode ?? 'All'}
          onChange={(v) => set({ mode: v === 'All' ? undefined : v })}
          options={[
            { value: 'All', label: 'All types' },
            { value: 'Practice', label: 'Practice' },
            { value: 'Exam', label: 'Simulation exam' },
          ]}
        />
      </Space>
      {list.isError ? (
        <Result status="warning" title={errorMessage(list.error)} />
      ) : (
        <Table<PracticeSummary>
          rowKey="id"
          size="middle"
          loading={list.isPending}
          columns={columns}
          dataSource={list.data?.items}
          scroll={{ x: 760 }}
          onChange={(_, __, sorter) => {
            const s = Array.isArray(sorter) ? sorter[0] : sorter
            set({ oldestFirst: s.order === 'ascend' || undefined })
          }}
          locale={{
            // A1: nothing taken yet; New Practice stays available.
            emptyText: filtered ? (
              <Empty description="No practice matches these filters.">
                <Button onClick={() => set({ subjectId: undefined, mode: undefined })}>Clear filters</Button>
              </Empty>
            ) : (
              <Empty description="You have not taken any practice yet.">
                <Button type="primary" onClick={() => navigate('/practices/new')}>
                  New Practice
                </Button>
              </Empty>
            ),
          }}
          pagination={{
            current: list.data?.pageNumber ?? criteria.page,
            pageSize: PAGE_SIZE,
            total: list.data?.totalCount ?? 0,
            hideOnSinglePage: true,
            showSizeChanger: false,
            onChange: (page) => set({ page }),
          }}
        />
      )}
    </Flex>
  )
}
