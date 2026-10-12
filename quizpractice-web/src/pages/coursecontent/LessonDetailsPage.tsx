import { useEffect } from 'react'
import { Alert, App, Button, Form, Input, InputNumber, Result, Select, Space, Spin } from 'antd'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import type { LessonInput } from '../../api/subjectsApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { RichTextEditor } from '../../components/RichTextEditor'
import { NAME_MAX, URL_MAX } from '../../constants/fieldLength'
import { useLesson, useLessonChoices, useSaveLesson } from '../../hooks/useSubjects'

/** Lesson Type settings the form knows (F#29); the code reads them by name. */
const TOPIC = 'Subject Topic'
const LESSON = 'Lesson'
const QUIZ = 'Quiz'

/**
 * Lesson Details (SRS III 6.7; PDF II.6, F#29; UC-50 Add or Edit Lesson). The type shapes the
 * form: a topic has neither video nor content, a lesson has both, a quiz points at a lesson quiz.
 */
export function LessonDetailsPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const params = useParams()
  const subjectId = Number(params.id)
  const lessonId = params.lessonId ? Number(params.lessonId) : undefined
  const isNew = lessonId === undefined

  const [form] = Form.useForm<LessonInput>()
  const stored = useLesson(subjectId, lessonId)
  const choices = useLessonChoices(subjectId)
  const save = useSaveLesson(subjectId, lessonId)

  const backTo = (location.state as { back?: string } | null)?.back ?? `/manage/subjects/${subjectId}/lessons`

  useEffect(() => {
    if (stored.data) {
      const { name, typeId, topicId, displayOrder, videoUrl, htmlContent, quizId } = stored.data
      form.setFieldsValue({ name, typeId, topicId, displayOrder, videoUrl: videoUrl ?? '', htmlContent: htmlContent ?? '', quizId })
    }
  }, [stored.data, form])

  // Fields of the chosen type only (step 4); the values the types share stay in the form (A2).
  const typeId = Form.useWatch('typeId', form) ?? stored.data?.typeId
  const types = choices.data?.types ?? []
  const typeName = types.find((t) => t.id === typeId)?.name ?? stored.data?.type
  const quizzes = (choices.data?.quizzes ?? []).filter((q) => q.status === 'Active' || q.id === stored.data?.quizId)

  if (!isNew && stored.isPending) return <Spin />
  if (!isNew && stored.isError) {
    const status = problemOf(stored.error)?.status
    return (
      <Result
        status={status === 404 ? '404' : status === 403 ? '403' : 'error'}
        title={status === 404 ? 'This lesson does not exist.' : errorMessage(stored.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Subject Lessons</Button>}
      />
    )
  }

  const onFinish = (values: LessonInput) =>
    save.mutate(values, {
      onSuccess: (lesson) => {
        message.success(isNew ? `Lesson "${lesson.name}" was added.` : `Lesson "${lesson.name}" was saved.`)
        navigate(backTo)
      },
      // E2: keep everything entered and mark the fields to correct.
      onError: (e) => {
        if (!applyFieldErrors(form, e)) message.error(errorMessage(e), 8)
      },
    })

  return (
    <>
      <PageHeader title={isNew ? 'New Lesson' : 'Lesson Details'} />
      {choices.data && <p>Subject: {choices.data.subject}</p>}
      <Form<LessonInput> form={form} name="lesson" layout="vertical" onFinish={onFinish} style={{ maxWidth: 880 }}>
        <Form.Item label="Lesson Name" name="name" rules={[{ required: true, whitespace: true, message: 'Enter the lesson name.' }, { max: NAME_MAX }]}>
          <Input />
        </Form.Item>

        <Space size="large" align="start" wrap>
          <Form.Item label="Type" name="typeId" rules={[{ required: true, message: 'Choose the lesson type.' }]}>
            <Select
              options={types.map((t) => ({ value: t.id, label: t.name }))}
              loading={choices.isPending}
              placeholder="Choose a type"
              style={{ width: 200 }}
            />
          </Form.Item>
          {typeName && typeName !== TOPIC && (
            <Form.Item label="Topic" name="topicId" rules={[{ required: true, message: 'Choose the Subject Topic this lesson sits under.' }]}>
              <Select
                options={(choices.data?.topics ?? []).filter((t) => t.id !== lessonId).map((t) => ({ value: t.id, label: t.name }))}
                placeholder="Choose a topic"
                style={{ width: 320 }}
              />
            </Form.Item>
          )}
          <Form.Item label="Order" name="displayOrder" extra="Empty: after the last one.">
            <InputNumber min={1} precision={0} style={{ width: 120 }} />
          </Form.Item>
        </Space>

        {typeName === LESSON && (
          <>
            <Form.Item
              label="Video Link"
              name="videoUrl"
              rules={[{ max: URL_MAX }, { type: 'url', message: 'Enter a full https:// address.' }]}
              extra="Address of the video, such as a YouTube link."
            >
              <Input placeholder="https://" />
            </Form.Item>
            <Form.Item label="HTML Content" name="htmlContent">
              <RichTextEditor />
            </Form.Item>
          </>
        )}

        {typeName === QUIZ &&
          (quizzes.length === 0 ? (
            // E3: no lesson quiz yet; offer Quizzes List.
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 24 }}
              title="This subject has no lesson quiz yet."
              description={
                <>
                  Prepare one first in <Link to="/manage/quizzes">Quizzes List</Link>, with the type Lesson quiz.
                </>
              }
            />
          ) : (
            <Form.Item label="Quiz" name="quizId" rules={[{ required: true, message: 'Choose a lesson quiz of this subject.' }]}>
              <Select options={quizzes.map((q) => ({ value: q.id, label: q.name }))} placeholder="Choose a quiz" style={{ width: 400 }} />
            </Form.Item>
          ))}

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
