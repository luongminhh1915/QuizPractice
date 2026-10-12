import { Button, Flex, Input, Result, Select, Space, Spin, Typography } from 'antd'
import { useNavigate } from 'react-router'
import type { ExamSummary } from '../../api/examApi'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { useExamFilters, useExams } from '../../hooks/useExams'
import { useListParams } from '../../hooks/useListParams'

/**
 * Simulation Exams (SRS III 3.4; F#16; UC-22, UC-23): the active simulation exams of the
 * subjects the customer may reach (BR-05, BR-23), searched by name and filtered by subject.
 */
export function SimulationExamsPage() {
  const navigate = useNavigate()
  const { criteria, get, set } = useListParams()
  const subjectId = get('subjectId') ? Number(get('subjectId')) : undefined
  const list = useExams({ ...criteria, subjectId })
  const filters = useExamFilters()

  const columns: DataTableColumn<ExamSummary>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 70 },
    { title: 'Subject', key: 'subject', sortable: true, render: (e) => e.subjectName },
    { title: 'Exam', key: 'name', sortable: true },
    { title: 'Level', key: 'level', sortable: true },
    { title: 'Questions', key: 'questionCount', width: 100 },
    { title: 'Duration', key: 'durationMin', width: 100, render: (e) => `${e.durationMin} min` },
    { title: 'Pass Rate', key: 'passRate', width: 100, render: (e) => `${e.passRate}%` },
  ]

  if (filters.isPending) return <Spin />
  // E2: no subject with valid access.
  if (filters.data?.subjects.length === 0) {
    return (
      <Result
        status="info"
        title="You have no subject to take an exam in yet."
        subTitle="The exams of a subject open once its registration is paid and still valid."
        extra={
          <Button type="primary" onClick={() => navigate('/subjects')}>
            Subjects List
          </Button>
        }
      />
    )
  }

  return (
    <Flex vertical gap={16}>
      <Flex justify="space-between" align="center" wrap gap={8}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Simulation Exams
        </Typography.Title>
        <Button onClick={() => navigate('/practices')}>Practices List</Button>
      </Flex>
      <Space wrap>
        <Input.Search
          allowClear
          placeholder="Search by exam name"
          defaultValue={criteria.search}
          onSearch={(v) => set({ search: v.trim() || undefined })}
          style={{ width: 280 }}
        />
        <Select<number | 'All'>
          style={{ width: 280 }}
          value={subjectId ?? 'All'}
          onChange={(v) => set({ subjectId: v === 'All' ? undefined : v })}
          options={[
            { value: 'All' as const, label: 'All subjects' },
            ...(filters.data?.subjects ?? []).map((s) => ({ value: s.id, label: s.name })),
          ]}
        />
      </Space>
      {list.isError ? (
        <Result status="warning" title={errorMessage(list.error)} />
      ) : (
        <DataTable<ExamSummary>
          columns={columns}
          page={list.data}
          loading={list.isPending}
          criteria={criteria}
          onChange={set}
          // E1: nothing matches; the criteria stay and can be cleared (A1).
          hasCriteria={Boolean(criteria.search) || subjectId !== undefined}
          onClearCriteria={() => set({ search: undefined, subjectId: undefined })}
        />
      )}
    </Flex>
  )
}
