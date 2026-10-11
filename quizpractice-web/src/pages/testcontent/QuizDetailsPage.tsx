import { useEffect, useState } from 'react'
import { Alert, App, Button, Flex, Form, Input, InputNumber, Radio, Result, Select, Space, Spin, Tabs, Typography } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { useLocation, useNavigate, useParams } from 'react-router'
import type { QuizInput } from '../../api/quizzesApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { LONG_TEXT_MAX, NAME_MAX } from '../../constants/fieldLength'
import { useChangeQuizStatus, useQuiz, useQuizChoices, useQuizGroups, useSaveQuiz } from '../../hooks/useQuizzes'

type RuleRow = { groupId?: number; questionCount?: number }
type FormValues = Omit<QuizInput, 'rules'> & { rules: RuleRow[] }

/** Fields of the Setting tab: an error on one of them opens that tab. */
const SETTING_FIELDS = ['questionCount', 'selectionType', 'rules']

const TOPIC = 'Topic'

/**
 * Quiz Details (SRS III 7.5; PDF II.7, F#34; UC-57 Add or Edit Quiz). One form on two tabs,
 * Overview and Setting, saved together: /manage/quizzes/new opens it empty, /manage/quizzes/:id filled.
 */
export function QuizDetailsPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const params = useParams()
  const id = params.id ? Number(params.id) : undefined
  const isNew = id === undefined

  const [form] = Form.useForm<FormValues>()
  const [tab, setTab] = useState('overview')
  const [rulesErrors, setRulesErrors] = useState<string[]>([])
  const stored = useQuiz(id)
  const choices = useQuizChoices()
  const save = useSaveQuiz(id)
  const changeStatus = useChangeQuizStatus(id ?? 0)

  const subjectId = Form.useWatch('subjectId', form)
  const drawnBy = Form.useWatch('selectionType', form)
  const rules = Form.useWatch('rules', form) as RuleRow[] | undefined
  const questionCount = Form.useWatch('questionCount', form) as number | undefined
  const groups = useQuizGroups(subjectId, drawnBy)
  const available = new Map((groups.data?.groups ?? []).map((g) => [g.id, g.available]))

  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/quizzes'
  // UC-57 A2 (BR-13): a taken quiz is shown read only; only its status may change.
  const taken = stored.data?.isTaken === true
  // Form disabled={taken} covers every control except those given their own disabled: those add `taken` themselves.

  useEffect(() => {
    if (stored.data) {
      const q = stored.data
      form.setFieldsValue({
        name: q.name,
        status: q.status,
        subjectId: q.subjectId,
        levelId: q.levelId,
        durationMin: q.durationMin,
        passRate: q.passRate,
        typeId: q.typeId,
        description: q.description ?? '',
        questionCount: q.questionCount,
        selectionType: q.selectionType,
        rules: q.rules.map((r) => ({ groupId: r.groupId, questionCount: r.questionCount })),
      })
    }
  }, [stored.data, form])

  if (!isNew && stored.isPending) return <Spin />
  if (!isNew && stored.isError) {
    const status = problemOf(stored.error)?.status
    return (
      <Result
        status={status === 404 ? '404' : status === 403 ? '403' : 'error'}
        title={status === 404 ? 'This quiz does not exist.' : errorMessage(stored.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Quizzes List</Button>}
      />
    )
  }

  const total = (rules ?? []).reduce((sum, r) => sum + (r?.questionCount ?? 0), 0)
  const chosenGroups = new Set((rules ?? []).map((r) => r?.groupId).filter((g) => g !== undefined))

  // UC-57 A4: the groups belong to a subject and a way of drawing; a new one clears them.
  const restartGroups = () => {
    form.setFieldValue('rules', [{}])
    setRulesErrors([])
  }

  const saveStatus = () => {
    const status = form.getFieldValue('status') as QuizInput['status']
    changeStatus.mutate(status, {
      onSuccess: (quiz) => {
        message.success(`Quiz "${quiz.name}" is now ${quiz.status}.`)
        navigate(backTo)
      },
      onError: (e) => message.error(errorMessage(e), 8),
    })
  }

  const onFinish = (values: FormValues) => {
    // E3 before the request: the API checks it again.
    if (total !== values.questionCount) {
      setRulesErrors([`The groups add up to ${total} questions, not ${values.questionCount}.`])
      setTab('setting')
      return
    }

    setRulesErrors([])
    const input: QuizInput = { ...values, rules: values.rules.map((r) => ({ groupId: r.groupId!, questionCount: r.questionCount! })) }
    save.mutate(input, {
      onSuccess: (quiz) => {
        const drawn = quiz.fixedQuestionCount ? ` ${quiz.fixedQuestionCount} questions were drawn for it.` : ''
        message.success(`Quiz "${quiz.name}" was ${isNew ? 'added' : 'saved'}.${drawn}`)
        navigate(backTo)
      },
      onError: (e) => {
        const problem = problemOf(e)
        // E5: the first attempt arrived meanwhile; the reload opens the quiz read only.
        if (problem?.code === 'Quiz.Taken') {
          message.warning(errorMessage(e), 8)
          void stored.refetch()
          return
        }

        // E2, E3 from the API: the groups are on the Setting tab.
        const groupErrors = problem?.errors?.Rules ?? problem?.errors?.SelectionType
        if (groupErrors) {
          setRulesErrors(groupErrors)
          setTab('setting')
        }

        const shown = applyFieldErrors(form, e)
        const fields = Object.keys(problem?.errors ?? {}).map((k) => k.charAt(0).toLowerCase() + k.slice(1))
        if (fields.length && fields.every((f) => SETTING_FIELDS.includes(f))) setTab('setting')
        else if (fields.length) setTab('overview')
        if (!shown && !groupErrors) message.error(errorMessage(e), 8)
      },
    })
  }

  const overview = (
    <>
      <Flex gap={16} wrap>
        <Form.Item label="Name" name="name" rules={[{ required: true, whitespace: true, message: 'Enter the quiz name.' }, { max: NAME_MAX }]} style={{ flex: '1 1 360px' }}>
          <Input />
        </Form.Item>
        <Form.Item label="Status" name="status" extra="Only an active simulation exam is offered to customers.">
          {/* The one field that stays changeable on a taken quiz (A2, BR-23). */}
          <Radio.Group optionType="button" options={['Active', 'Inactive']} disabled={false} />
        </Form.Item>
      </Flex>
      <Flex gap={16} wrap>
        <Form.Item label="Subject" name="subjectId" rules={[{ required: true, message: 'Choose a subject.' }]} style={{ width: 320 }}>
          <Select
            placeholder="Choose a subject"
            loading={choices.isPending}
            options={(choices.data?.subjects ?? []).map((s) => ({ value: s.id, label: s.name }))}
            onChange={(value: number) => {
              // BR-21: a new quiz starts from the subject's pass rate.
              const subject = choices.data?.subjects.find((s) => s.id === value)
              if (isNew && subject) form.setFieldValue('passRate', subject.passRate)
              form.setFieldValue('selectionType', TOPIC)
              restartGroups()
            }}
          />
        </Form.Item>
        <Form.Item label="Level" name="levelId" rules={[{ required: true, message: 'Choose a level.' }]} style={{ width: 160 }}>
          <Select placeholder="Level" options={(choices.data?.levels ?? []).map((l) => ({ value: l.id, label: l.name }))} />
        </Form.Item>
        <Form.Item label="Duration (min)" name="durationMin" rules={[{ required: true, message: 'Enter the duration.' }]}>
          <InputNumber min={1} max={180} precision={0} style={{ width: 130 }} />
        </Form.Item>
        <Form.Item label="Pass Rate (%)" name="passRate" rules={[{ required: true, message: 'Enter the pass rate.' }]}>
          <InputNumber min={0} max={100} precision={0} style={{ width: 130 }} />
        </Form.Item>
      </Flex>
      <Form.Item
        label="Quiz Type"
        name="typeId"
        rules={[{ required: true, message: 'Choose the quiz type.' }]}
        extra="A simulation exam draws a new set at every attempt; a lesson quiz draws its set once, when it is saved."
      >
        <Radio.Group optionType="button" options={(choices.data?.types ?? []).map((t) => ({ value: t.id, label: t.name }))} />
      </Form.Item>
      <Form.Item label="Description" name="description">
        <Input.TextArea rows={3} showCount maxLength={LONG_TEXT_MAX} />
      </Form.Item>
    </>
  )

  const setting = (
    <>
      <Flex gap={16} wrap>
        <Form.Item label="Number of Questions" name="questionCount" rules={[{ required: true, message: 'Enter the number of questions.' }]}>
          <InputNumber min={1} max={100} precision={0} style={{ width: 160 }} />
        </Form.Item>
        <Form.Item label="Drawn By" name="selectionType" rules={[{ required: true }]} style={{ width: 220 }}>
          <Select
            disabled={taken || !subjectId}
            options={(groups.data?.drawnBy ?? [TOPIC]).map((d) => ({ value: d, label: d === TOPIC ? 'Topic' : `Dimension: ${d}` }))}
            onChange={restartGroups}
          />
        </Form.Item>
      </Flex>

      <Form.List name="rules">
        {(fields, { add, remove }) => (
          <Flex vertical gap={8} style={{ maxWidth: 760 }}>
            <Flex gap={8} style={{ fontWeight: 600 }}>
              <span style={{ flex: 1 }}>Group</span>
              <span style={{ width: 120 }}>Questions</span>
              <span style={{ width: 90 }}>Available</span>
              <span style={{ width: 32 }} />
            </Flex>
            {fields.map((field) => {
              const groupId = rules?.[field.name]?.groupId
              const count = rules?.[field.name]?.questionCount
              const holds = groupId !== undefined ? available.get(groupId) : undefined
              return (
                <Flex key={field.key} gap={8} align="start">
                  <Form.Item name={[field.name, 'groupId']} rules={[{ required: true, message: 'Choose a group.' }]} style={{ flex: 1, marginBottom: 0 }}>
                    <Select
                      aria-label={`Group ${field.name + 1}`}
                      placeholder={subjectId ? 'Choose a group' : 'Choose a subject first'}
                      disabled={taken || !subjectId}
                      loading={groups.isFetching}
                      // E3: a group may appear once.
                      options={(groups.data?.groups ?? []).map((g) => ({
                        value: g.id,
                        label: g.name,
                        disabled: chosenGroups.has(g.id) && g.id !== groupId,
                      }))}
                    />
                  </Form.Item>
                  <Form.Item name={[field.name, 'questionCount']} rules={[{ required: true, message: 'How many?' }]} style={{ width: 120, marginBottom: 0 }}>
                    <InputNumber aria-label={`Questions of group ${field.name + 1}`} min={1} max={100} precision={0} style={{ width: '100%' }} />
                  </Form.Item>
                  {/* E2: more than the group holds is refused on Save. */}
                  <Typography.Text
                    type={holds !== undefined && count !== undefined && count > holds ? 'danger' : 'secondary'}
                    style={{ width: 90, paddingTop: 5 }}
                  >
                    {holds ?? '—'}
                  </Typography.Text>
                  <Button icon={<DeleteOutlined />} aria-label={`Remove group ${field.name + 1}`} disabled={taken || fields.length <= 1} onClick={() => remove(field.name)} />
                </Flex>
              )
            })}
            <Flex justify="space-between" align="center">
              <Button icon={<PlusOutlined />} disabled={taken || !subjectId} onClick={() => add({})}>
                Add Group
              </Button>
              {/* E3: the counts must add up to the number of questions. */}
              <Typography.Text strong type={questionCount && total === questionCount ? 'success' : 'danger'}>
                Group total: {total} of {questionCount ?? '—'} questions
              </Typography.Text>
            </Flex>
          </Flex>
        )}
      </Form.List>

      {rulesErrors.length > 0 && (
        <Alert
          type="error"
          showIcon
          style={{ marginTop: 16, maxWidth: 760 }}
          title="The groups cannot be saved"
          description={
            <ul style={{ margin: 0, paddingLeft: 18 }}>
              {rulesErrors.map((m) => (
                <li key={m}>{m}</li>
              ))}
            </ul>
          }
        />
      )}
      {stored.data && stored.data.fixedQuestionCount > 0 && (
        <Typography.Paragraph type="secondary" style={{ marginTop: 16 }}>
          This lesson quiz holds a fixed set of {stored.data.fixedQuestionCount} questions. Saving draws a new set only when the
          subject, the way of drawing, the number of questions or the groups change.
        </Typography.Paragraph>
      )}
    </>
  )

  return (
    <>
      <PageHeader title={isNew ? 'New Quiz' : `Quiz Details #${id}`} />
      {taken && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16, maxWidth: 900 }}
          title="Customers have taken this quiz"
          description="It is shown read only so that every stored result keeps the quiz it was scored against (BR-13). Only the status can still change."
        />
      )}
      <Form<FormValues>
        form={form}
        disabled={taken}
        layout="vertical"
        initialValues={{ status: 'Active', selectionType: TOPIC, rules: [{}] }}
        onFinish={onFinish}
        onFinishFailed={({ errorFields }) => {
          const first = String(errorFields[0]?.name[0] ?? '')
          setTab(SETTING_FIELDS.includes(first) ? 'setting' : 'overview')
        }}
        style={{ maxWidth: 900 }}
      >
        <Tabs
          activeKey={tab}
          onChange={setTab}
          items={[
            { key: 'overview', label: 'Overview', children: overview, forceRender: true },
            { key: 'setting', label: 'Setting', children: setting, forceRender: true },
          ]}
        />
        <Space style={{ marginTop: 8 }}>
          {taken ? (
            <Button type="primary" disabled={false} loading={changeStatus.isPending} onClick={saveStatus}>
              Save status
            </Button>
          ) : (
            <Button type="primary" htmlType="submit" loading={save.isPending}>
              Save
            </Button>
          )}
          <Button disabled={false} onClick={() => navigate(backTo)}>
            Cancel
          </Button>
        </Space>
      </Form>
    </>
  )
}
