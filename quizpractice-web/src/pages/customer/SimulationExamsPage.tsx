import { useState } from 'react'
import { Alert, App, Button, Descriptions, Flex, Input, Modal, Result, Select, Space, Spin, Typography } from 'antd'
import { useNavigate } from 'react-router'
import type { ExamSummary } from '../../api/examApi'
import { errorMessage, problemOf } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { useExam, useExamFilters, useExams, useStartExam } from '../../hooks/useExams'
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
  const [taking, setTaking] = useState<number>()

  const columns: DataTableColumn<ExamSummary>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 70 },
    { title: 'Subject', key: 'subject', sortable: true, render: (e) => e.subjectName },
    { title: 'Exam', key: 'name', sortable: true },
    { title: 'Level', key: 'level', sortable: true },
    { title: 'Questions', key: 'questionCount', width: 100 },
    { title: 'Duration', key: 'durationMin', width: 100, render: (e) => `${e.durationMin} min` },
    { title: 'Pass Rate', key: 'passRate', width: 100, render: (e) => `${e.passRate}%` },
    {
      title: 'Action',
      key: 'action',
      width: 110,
      render: (e) => (
        <Button size="small" type="primary" ghost onClick={() => setTaking(e.id)}>
          Take Exam
        </Button>
      ),
    },
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
      <ExamStartModal id={taking} onClose={() => setTaking(undefined)} />
    </Flex>
  )
}

/**
 * Exam details before the start (UC-23 steps 2 and 3). Start Exam draws a new set and
 * opens Quiz Handle; an unfinished attempt at the same exam is resumed instead (A2).
 */
function ExamStartModal({ id, onClose }: { id: number | undefined; onClose: () => void }) {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const exam = useExam(id)
  const start = useStartExam()
  const e = exam.data

  const startExam = () =>
    start.mutate(id!, {
      onSuccess: (attempt) => navigate(`/quiz/${attempt.id}`),
      onError: (err) => {
        const code = problemOf(err)?.code
        message.warning(errorMessage(err))
        // E1: the access ended since the list loaded; the list reloads without the exam.
        if (code === 'Subject.NoAccess') onClose()
        // A2 from another tab: the details reload with Resume.
        if (code === 'Exam.Unfinished') void exam.refetch()
      },
    })

  return (
    <Modal
      open={id !== undefined}
      title={e ? e.name : 'Simulation Exam'}
      onCancel={onClose}
      destroyOnHidden
      footer={
        e && (
          <Space>
            <Button onClick={onClose}>Cancel</Button>
            {e.openAttemptId !== null ? (
              <Button type="primary" onClick={() => navigate(`/quiz/${e.openAttemptId}`)}>
                Resume
              </Button>
            ) : (
              <Button type="primary" loading={start.isPending} onClick={startExam}>
                Start Exam
              </Button>
            )}
          </Space>
        )
      }
    >
      {exam.isPending ? (
        <Spin />
      ) : exam.isError ? (
        <Alert type="warning" showIcon title={errorMessage(exam.error)} />
      ) : (
        <Flex vertical gap={16}>
          <Descriptions column={1} bordered size="small">
            <Descriptions.Item label="Subject">{e!.subjectName}</Descriptions.Item>
            <Descriptions.Item label="Level">{e!.level}</Descriptions.Item>
            <Descriptions.Item label="Questions">{e!.questionCount}</Descriptions.Item>
            <Descriptions.Item label="Duration">{e!.durationMin} minutes</Descriptions.Item>
            <Descriptions.Item label="Pass Rate">{e!.passRate}%</Descriptions.Item>
          </Descriptions>
          {e!.description && <Typography.Paragraph style={{ margin: 0 }}>{e!.description}</Typography.Paragraph>}
          {/* Exam Rules (BR-06, BR-07). */}
          <Alert
            type="info"
            showIcon
            title="Exam rules"
            description="The clock counts down over the duration and the exam is submitted when it reaches zero. Peek at Answer is not available. Every start draws a new set of questions."
          />
          {e!.openAttemptId !== null && (
            <Alert type="warning" showIcon title="You have an unfinished attempt at this exam. Resume it to carry on where you left off." />
          )}
        </Flex>
      )}
    </Modal>
  )
}
