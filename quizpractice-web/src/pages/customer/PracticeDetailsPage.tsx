import { App, Button, Card, Descriptions, Flex, Form, InputNumber, Radio, Result, Select, Spin, Tag, Typography } from 'antd'
import dayjs from 'dayjs'
import { useNavigate, useParams } from 'react-router'
import type { SelectionType } from '../../api/practiceApi'
import { applyFieldErrors, errorMessage } from '../../api/client'
import { useCreatePractice, usePractice, usePracticeForm, usePracticeGroups } from '../../hooks/usePractices'
import { formatDuration } from '../../utils/duration'
import { groupedOptions } from '../../utils/groupedOptions'

interface PracticeFields {
  subjectId?: number
  questionCount?: number
  selectionType: SelectionType
  groupId?: number
}

const ALL = 0

/**
 * Practice Details (SRS III 3.3; F#15). New Practice (UC-20): a subject the customer may
 * practise, the number of questions, by topic or by dimension, and one group or all of
 * them. The screen counts the matching questions as the choice changes, so the number
 * can be lowered before Practice (E2, A2). View Details (UC-21) opens the same screen read
 * only, with the result and Practice Review in place of Practice; it also opens right after
 * an attempt is scored (UC-29 step 7).
 */
export function PracticeDetailsPage() {
  const { id } = useParams()
  return id ? <ExistingPractice id={Number(id)} /> : <NewPractice />
}

function ExistingPractice({ id }: { id: number }) {
  const navigate = useNavigate()
  const practice = usePractice(id)

  if (practice.isPending) return <Spin />
  // E1: no longer there (or someone else's).
  if (practice.isError) {
    return (
      <Result
        status="warning"
        title={errorMessage(practice.error)}
        extra={<Button onClick={() => navigate('/practices')}>Practices List</Button>}
      />
    )
  }

  const p = practice.data
  const outcome = p.outcome
  return (
    <Flex vertical gap={16} style={{ maxWidth: 720, margin: '0 auto' }}>
      <Typography.Title level={3} style={{ margin: 0 }}>
        Practice Details
      </Typography.Title>
      <Card>
        <Descriptions column={1} bordered size="middle">
          <Descriptions.Item label="Subject">{p.subjectName}</Descriptions.Item>
          {p.mode === 'Exam' ? (
            <Descriptions.Item label="Simulation Exam">{p.title}</Descriptions.Item>
          ) : (
            <>
              <Descriptions.Item label="Selected By">{p.selectionType === 'Topic' ? 'By topic' : 'By dimension'}</Descriptions.Item>
              <Descriptions.Item label="Question Group">{p.groupName}</Descriptions.Item>
            </>
          )}
          <Descriptions.Item label="Number of Questions">{p.questionCount}</Descriptions.Item>
          <Descriptions.Item label="Date Taken">{dayjs(p.startedAt).format('DD/MM/YYYY HH:mm')}</Descriptions.Item>
          {outcome ? (
            <Descriptions.Item label="Result">
              <Flex vertical gap={4}>
                <span>
                  <Typography.Text strong>
                    {outcome.correct} / {p.questionCount}
                  </Typography.Text>{' '}
                  correct ({outcome.score}%) in {formatDuration(outcome.durationSeconds * 1000)}
                </span>
                {/* BR-21: judged against the pass rate stored with the attempt. */}
                <span>
                  <Tag color={outcome.passed ? 'green' : 'red'}>{outcome.passed ? 'Passed' : 'Failed'}</Tag>
                  <Typography.Text type="secondary">pass rate {outcome.passRate}%</Typography.Text>
                </span>
              </Flex>
            </Descriptions.Item>
          ) : (
            <Descriptions.Item label="Result">
              <Tag color="blue">In progress</Tag>
            </Descriptions.Item>
          )}
        </Descriptions>
        <Flex gap={8} justify="end" style={{ marginTop: 16 }}>
          <Button onClick={() => navigate('/practices')}>Practices List</Button>
          {/* Practice Review replaces Practice (UC-21 step 3); an unfinished attempt continues instead. */}
          {outcome ? (
            <Button type="primary" onClick={() => navigate(`/quiz/${p.id}/review`)}>
              Practice Review
            </Button>
          ) : (
            <Button type="primary" onClick={() => navigate(`/quiz/${p.id}`)}>
              Continue
            </Button>
          )}
        </Flex>
      </Card>
    </Flex>
  )
}

function NewPractice() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const [form] = Form.useForm<PracticeFields>()
  const subjectId = Form.useWatch('subjectId', form)
  const selectionType = Form.useWatch('selectionType', form) ?? 'Topic'
  const groupId = Form.useWatch('groupId', form) ?? ALL
  const choices = usePracticeForm()
  const groups = usePracticeGroups(subjectId, selectionType)
  const create = useCreatePractice()

  const matching = groupId === ALL ? groups.data?.total : groups.data?.groups.find((g) => g.id === groupId)?.available
  const groupOptions = [
    { value: ALL, label: selectionType === 'Topic' ? 'All topics' : 'All dimensions' },
    ...(selectionType === 'Topic'
      ? (groups.data?.groups ?? []).map((g) => ({ value: g.id, label: g.name }))
      : groupedOptions(groups.data?.groups ?? [], (g) => g.type ?? '')),
  ]

  const submit = (values: PracticeFields) =>
    create.mutate(
      {
        subjectId: values.subjectId!,
        questionCount: values.questionCount!,
        selectionType: values.selectionType,
        groupId: values.groupId === ALL ? undefined : values.groupId,
      },
      {
        onSuccess: (attempt) => navigate(`/quiz/${attempt.id}`),
        // E2 lands under Number of Questions; anything else in the message area.
        onError: (e) => {
          if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
        },
      },
    )

  if (choices.isPending) return <Spin />
  if (choices.isError) return <Result status="warning" title={errorMessage(choices.error)} />

  // E1: nothing to practise yet.
  if (choices.data.subjects.length === 0) {
    return (
      <Result
        status="info"
        title="You have no subject to practise yet."
        subTitle="A subject opens for practice once its registration is paid and still valid."
        extra={
          <Button type="primary" onClick={() => navigate('/subjects')}>
            Subjects List
          </Button>
        }
      />
    )
  }

  return (
    <Flex vertical gap={16} style={{ maxWidth: 640, margin: '0 auto' }}>
      <Typography.Title level={3} style={{ margin: 0 }}>
        New Practice
      </Typography.Title>
      <Card>
        <Form<PracticeFields>
          form={form}
          layout="vertical"
          initialValues={{ selectionType: 'Topic', groupId: ALL, questionCount: 10 }}
          onFinish={submit}
          onValuesChange={(changed) => {
            // Another subject or another way of selecting has other groups.
            if ('subjectId' in changed || 'selectionType' in changed) form.setFieldValue('groupId', ALL)
          }}
        >
          <Form.Item name="subjectId" label="Subject" rules={[{ required: true, message: 'Choose a subject.' }]}>
            <Select
              placeholder="Choose a subject"
              options={choices.data.subjects.map((s) => ({ value: s.id, label: s.name }))}
            />
          </Form.Item>
          <Form.Item name="selectionType" label="Selected By">
            <Radio.Group
              optionType="button"
              options={[
                { value: 'Topic', label: 'By topic' },
                { value: 'Dimension', label: 'By dimension' },
              ]}
            />
          </Form.Item>
          <Form.Item name="groupId" label="Question Group">
            <Select disabled={subjectId === undefined} loading={groups.isFetching} options={groupOptions} />
          </Form.Item>
          <Form.Item
            name="questionCount"
            label="Number of Questions"
            rules={[{ required: true, message: 'Enter how many questions to practise.' }]}
            extra={
              subjectId !== undefined && matching !== undefined
                ? `${matching} question${matching === 1 ? '' : 's'} match this choice.`
                : undefined
            }
          >
            <InputNumber min={1} max={100} precision={0} style={{ width: 160 }} />
          </Form.Item>
          <Flex gap={8} justify="end">
            <Button onClick={() => navigate('/practices')}>Cancel</Button>
            <Button type="primary" htmlType="submit" loading={create.isPending} disabled={matching === 0}>
              Practice
            </Button>
          </Flex>
        </Form>
      </Card>
    </Flex>
  )
}
