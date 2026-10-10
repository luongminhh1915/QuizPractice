import { useEffect, useState } from 'react'
import { App, Button, Flex, Form, Input, Radio, Result, Select, Space, Spin } from 'antd'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { useLocation, useNavigate, useParams } from 'react-router'
import type { QuestionInput } from '../../api/questionsApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { QuestionMediaInput } from '../../components/QuestionMediaInput'
import { ANSWER_OPTION_MAX, LONG_TEXT_MAX, MAX_OPTIONS, MIN_OPTIONS } from '../../constants/fieldLength'
import { useQuestion, useQuestionFilters, useSaveQuestion } from '../../hooks/useQuestions'
import { groupedOptions } from '../../utils/groupedOptions'

type FormValues = Omit<QuestionInput, 'removeMedia'>

/** 409 answers that are not about the version: the user must change the request, not reload. */
const REFUSED = ['Question.InUse', 'Question.OptionAnswered']

/**
 * Question Details (SRS III 7.2; PDF II.7, F#31; UC-53 Add or Edit Question). One form
 * for add and edit (SRS III 0.4): /manage/questions/new opens it empty, /manage/questions/:id filled.
 */
export function QuestionDetailsPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const params = useParams()
  const id = params.id ? Number(params.id) : undefined
  const isNew = id === undefined

  const [form] = Form.useForm<FormValues>()
  const stored = useQuestion(id)
  const subjectId = Form.useWatch('subjectId', form)
  const filters = useQuestionFilters(subjectId)
  const save = useSaveQuestion(id)
  const choices = filters.data

  // "Remove" on the stored media holds for the version it was pressed on, so reloading
  // the question after E4 brings the stored media back by itself.
  const [mediaRemovedFor, setMediaRemovedFor] = useState<string>()
  const mediaRemoved = !isNew && mediaRemovedFor === stored.data?.version

  // Back to the list with the criteria it had (SRS III 0.3), or to the plain list.
  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/questions'

  useEffect(() => {
    if (stored.data) {
      const q = stored.data
      form.setFieldsValue({
        subjectId: q.subjectId,
        lessonId: q.lessonId ?? undefined,
        levelId: q.levelId,
        status: q.status,
        dimensionIds: q.dimensionIds,
        content: q.content,
        explanation: q.explanation ?? '',
        options: q.options.map((o) => ({ id: o.id, content: o.content })),
        correctIndex: q.options.findIndex((o) => o.isCorrect),
        media: undefined,
      })
    }
  }, [stored.data, form])

  if (!isNew && stored.isPending) return <Spin />
  if (!isNew && stored.isError) {
    const status = problemOf(stored.error)?.status
    return (
      <Result
        status={status === 404 ? '404' : status === 403 ? '403' : 'error'}
        title={status === 404 ? 'This question does not exist.' : errorMessage(stored.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Questions List</Button>}
      />
    )
  }

  // UC-53 A2: removing the option marked correct clears the mark, so another must be chosen.
  const removeOption = (index: number, remove: () => void) => {
    const correct = form.getFieldValue('correctIndex') as number | undefined
    remove()
    if (correct === index) form.setFieldValue('correctIndex', undefined)
    else if (correct !== undefined && correct > index) form.setFieldValue('correctIndex', correct - 1)
  }

  const onFinish = (values: FormValues) => {
    save.mutate(
      { input: { ...values, removeMedia: mediaRemoved }, version: stored.data?.version },
      {
        onSuccess: (question) => {
          message.success(isNew ? `Question #${question.id} was added.` : `Question #${question.id} was saved.`)
          navigate(backTo)
        },
        onError: (e) => {
          const code = problemOf(e)?.code
          // UC-53 E4: reload the current values and ask for the change again.
          if (code === 'Question.Changed') {
            message.warning(errorMessage(e), 6)
            void stored.refetch()
            return
          }
          if (code && REFUSED.includes(code)) {
            message.error(errorMessage(e), 8)
            return
          }
          // E2 from the API (count of options, the one correct mark) goes under the options.
          const optionErrors = problemOf(e)?.errors?.Options
          if (optionErrors) form.setFields([{ name: 'correctIndex', errors: optionErrors }])
          // E3 and the other field rules: keep everything entered and mark the fields.
          if (!applyFieldErrors(form, e) && !optionErrors) message.error(errorMessage(e))
        },
      },
    )
  }

  return (
    <>
      <PageHeader title={isNew ? 'New Question' : `Question Details #${id}`} />
      <Form<FormValues>
        form={form}
        layout="vertical"
        initialValues={{ status: 'Shown', dimensionIds: [], options: [{ content: '' }, { content: '' }] }}
        onFinish={onFinish}
        style={{ maxWidth: 880 }}
      >
        <Flex gap={16} wrap>
          <Form.Item
            label="Subject"
            name="subjectId"
            rules={[{ required: true, message: 'Choose a subject.' }]}
            extra={stored.data?.isUsed ? 'Fixed: a quiz or a customer attempt uses this question.' : undefined}
            style={{ width: 300 }}
          >
            <Select
              placeholder="Choose a subject"
              disabled={stored.data?.isUsed}
              loading={filters.isPending}
              // The lessons and dimensions belong to the subject, so a new subject clears them.
              onChange={() => form.setFieldsValue({ lessonId: undefined, dimensionIds: [] })}
              options={(choices?.subjects ?? []).map((s) => ({ value: s.id, label: s.name }))}
            />
          </Form.Item>
          <Form.Item label="Lesson" name="lessonId" style={{ width: 260 }}>
            <Select
              placeholder={subjectId ? 'Choose a lesson' : 'Choose a subject first'}
              disabled={!subjectId}
              allowClear
              options={groupedOptions(choices?.lessons ?? [], (l) => l.topic)}
            />
          </Form.Item>
          <Form.Item label="Level" name="levelId" rules={[{ required: true, message: 'Choose a level.' }]} style={{ width: 140 }}>
            <Select placeholder="Level" options={(choices?.levels ?? []).map((l) => ({ value: l.id, label: l.name }))} />
          </Form.Item>
          <Form.Item label="Status" name="status" extra="Only a shown question is drawn into new practices and quizzes.">
            <Radio.Group optionType="button" options={['Shown', 'Hidden']} />
          </Form.Item>
        </Flex>

        <Form.Item label="Dimensions" name="dimensionIds">
          <Select
            mode="multiple"
            placeholder={subjectId ? 'Choose one or more dimensions' : 'Choose a subject first'}
            disabled={!subjectId}
            allowClear
            options={groupedOptions(choices?.dimensions ?? [], (d) => d.type)}
          />
        </Form.Item>

        <Form.Item label="Content" name="content" rules={[{ required: true, whitespace: true, message: 'Enter the question.' }]}>
          <Input.TextArea rows={4} showCount maxLength={LONG_TEXT_MAX} />
        </Form.Item>

        <Form.Item
          label="Media"
          name="media"
          extra="Optional. A JPG, PNG or WebP image up to 2 MB, an MP3 up to 10 MB or an MP4 up to 20 MB."
        >
          <QuestionMediaInput
            current={
              stored.data?.mediaUrl && stored.data.mediaKind
                ? { url: stored.data.mediaUrl, kind: stored.data.mediaKind }
                : undefined
            }
            removed={mediaRemoved}
            onRemovedChange={(removed) => setMediaRemovedFor(removed ? stored.data?.version : undefined)}
          />
        </Form.Item>

        <Form.Item
          label="Answer Options"
          required
          extra={`Mark the correct answer with the radio button. ${MIN_OPTIONS} to ${MAX_OPTIONS} options, in the order the customer sees them.`}
        >
          {/* correctIndex sits outside Form.List (a Form.Item inside it would be named
              options.correctIndex); noStyle shows its message under Answer Options. */}
          <Form.Item name="correctIndex" rules={[{ required: true, message: 'Mark the correct answer.' }]} noStyle>
            <Radio.Group style={{ width: '100%' }}>
              <Form.List name="options">
                {(fields, { add, remove }) => (
                  <Flex vertical gap={8} align="start">
                    <Flex vertical gap={8} style={{ width: '100%' }}>
                      {fields.map((field, index) => (
                        <Flex key={field.key} gap={8} align="start">
                          <Radio value={index} aria-label={`Option ${index + 1} is correct`} style={{ marginTop: 5 }} />
                          <Form.Item
                            name={[field.name, 'content']}
                            rules={[
                              { required: true, whitespace: true, message: 'Enter the option text.' },
                              { max: ANSWER_OPTION_MAX },
                            ]}
                            style={{ flex: 1, marginBottom: 0 }}
                          >
                            <Input placeholder={`Option ${index + 1}`} />
                          </Form.Item>
                          <Button
                            icon={<DeleteOutlined />}
                            aria-label={`Remove option ${index + 1}`}
                            disabled={fields.length <= MIN_OPTIONS}
                            onClick={() => removeOption(index, () => remove(field.name))}
                          />
                        </Flex>
                      ))}
                    </Flex>
                    <Button icon={<PlusOutlined />} disabled={fields.length >= MAX_OPTIONS} onClick={() => add({ content: '' })}>
                      Add Option
                    </Button>
                  </Flex>
                )}
              </Form.List>
            </Radio.Group>
          </Form.Item>
        </Form.Item>

        <Form.Item label="Explanation" name="explanation" extra="Shown in Quiz Review. Without one, the Explanation action is not offered.">
          <Input.TextArea rows={3} showCount maxLength={LONG_TEXT_MAX} />
        </Form.Item>

        <Space>
          <Button type="primary" htmlType="submit" loading={save.isPending}>
            Save
          </Button>
          <Button onClick={() => navigate(backTo)}>Cancel</Button>
        </Space>
      </Form>
    </>
  )
}
