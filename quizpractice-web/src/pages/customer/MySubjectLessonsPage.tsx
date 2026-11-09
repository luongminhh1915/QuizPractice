import { useState } from 'react'
import { Alert, App, Button, Card, Descriptions, Empty, Flex, Modal, Result, Space, Spin, Tag, Typography } from 'antd'
import { FormOutlined, ReadOutlined } from '@ant-design/icons'
import { useNavigate, useParams } from 'react-router'
import type { LearnerLesson } from '../../api/mySubjectsApi'
import { errorMessage, problemOf } from '../../api/client'
import { useStartLessonQuiz, useSubjectLessons } from '../../hooks/useMySubjects'

/**
 * Subject Lessons for the customer (SRS III 3.6; UC-71): the Active lessons of one subject
 * under their topics. A lesson of type Quiz offers Take Quiz, which asks the fixed set the
 * lesson quiz keeps (OI-07); an unfinished attempt at it is resumed instead (A1).
 */
export function MySubjectLessonsPage() {
  const navigate = useNavigate()
  const subjectId = Number(useParams().id)
  const lessons = useSubjectLessons(subjectId)
  const [taking, setTaking] = useState<LearnerLesson>()

  if (lessons.isPending) return <Spin />
  // E1: no valid access to the subject (it ended, or the subject is not theirs).
  if (lessons.isError) {
    return (
      <Result
        status="warning"
        title={errorMessage(lessons.error)}
        extra={<Button onClick={() => navigate('/my-subjects')}>My Subjects</Button>}
      />
    )
  }

  const data = lessons.data
  return (
    <Flex vertical gap={16}>
      <Flex justify="space-between" align="center" wrap gap={8}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          {data.subjectName}
        </Typography.Title>
        <Button onClick={() => navigate('/my-subjects')}>My Subjects</Button>
      </Flex>
      {data.topics.length === 0 ? (
        <Empty description="This subject has no lessons yet." />
      ) : (
        data.topics.map((t) => (
          <Card key={t.id ?? 'other'} title={t.name ?? 'Other lessons'} size="small">
            <Flex vertical>
              {t.lessons.map((l, i) => (
                <Flex
                  key={l.id}
                  className="lesson-row"
                  justify="space-between"
                  align="center"
                  gap={12}
                  style={{ padding: '10px 0', borderTop: i === 0 ? undefined : '1px solid rgba(5, 5, 5, 0.06)' }}
                >
                  <Flex gap={12} align="flex-start">
                    {l.quiz ? <FormOutlined style={{ marginTop: 4 }} /> : <ReadOutlined style={{ marginTop: 4 }} />}
                    <Flex vertical>
                      <Space>
                        <Typography.Text>{l.name}</Typography.Text>
                        <Tag>{l.type}</Tag>
                      </Space>
                      {l.quiz && (
                        <Typography.Text type="secondary" style={{ fontSize: 13 }}>
                          {`${l.quiz.name} · ${l.quiz.questionCount} questions · ${l.quiz.durationMin} min · pass rate ${l.quiz.passRate}%`}
                        </Typography.Text>
                      )}
                    </Flex>
                  </Flex>
                  {l.quiz && <QuizAction lesson={l} onTake={() => setTaking(l)} />}
                </Flex>
              ))}
            </Flex>
          </Card>
        ))
      )}
      <LessonQuizModal subjectId={subjectId} lesson={taking} onClose={() => setTaking(undefined)} />
    </Flex>
  )
}

function QuizAction({ lesson, onTake }: { lesson: LearnerLesson; onTake: () => void }) {
  const navigate = useNavigate()
  const quiz = lesson.quiz!
  if (quiz.openAttemptId !== null) {
    return (
      <Button size="small" type="primary" onClick={() => navigate(`/quiz/${quiz.openAttemptId}`)}>
        Resume
      </Button>
    )
  }
  if (!quiz.available) return <Tag>Not available</Tag>
  return (
    <Button size="small" type="primary" ghost onClick={onTake}>
      Take Quiz
    </Button>
  )
}

/** The lesson quiz before the start (UC-71 steps 3 and 4); Start opens Quiz Handle. */
function LessonQuizModal({
  subjectId,
  lesson,
  onClose,
}: {
  subjectId: number
  lesson: LearnerLesson | undefined
  onClose: () => void
}) {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const start = useStartLessonQuiz(subjectId)
  const quiz = lesson?.quiz

  const startQuiz = () =>
    start.mutate(lesson!.id, {
      onSuccess: (attempt) => navigate(`/quiz/${attempt.id}`),
      onError: (err) => {
        message.warning(errorMessage(err))
        // E1, E2, A1 from another tab: the lessons reload with the right button.
        if (problemOf(err)?.code) onClose()
      },
    })

  return (
    <Modal
      open={lesson !== undefined}
      title={quiz?.name}
      onCancel={onClose}
      destroyOnHidden
      footer={
        <Space>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="primary" loading={start.isPending} onClick={startQuiz}>
            Start Quiz
          </Button>
        </Space>
      }
    >
      {quiz && (
        <Flex vertical gap={16}>
          <Descriptions column={1} bordered size="small">
            <Descriptions.Item label="Lesson">{lesson!.name}</Descriptions.Item>
            <Descriptions.Item label="Questions">{quiz.questionCount}</Descriptions.Item>
            <Descriptions.Item label="Duration">{quiz.durationMin} minutes</Descriptions.Item>
            <Descriptions.Item label="Pass Rate">{quiz.passRate}%</Descriptions.Item>
          </Descriptions>
          <Alert
            type="info"
            showIcon
            title="Quiz rules"
            description="The clock counts down over the duration and the quiz is submitted when it reaches zero. Peek at Answer is not available. Every attempt asks the same questions, set by the expert of the subject."
          />
        </Flex>
      )}
    </Modal>
  )
}
