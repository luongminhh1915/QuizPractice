import { useEffect, useState } from 'react'
import { App, Button, Card, Flex, Radio, Result, Space, Spin, Tag, Typography } from 'antd'
import { ClockCircleOutlined } from '@ant-design/icons'
import { useNavigate, useParams } from 'react-router'
import { mediaUrl, type HandleQuestion, type QuizHandle } from '../../api/attemptApi'
import { errorMessage, problemOf } from '../../api/client'
import { useAnswer, useQuizHandle } from '../../hooks/useAttempt'
import { formatDuration } from '../../utils/duration'

/**
 * Quiz Handle (SRS III 4.1; F#17; UC-24 to UC-30): one question at a time of a running
 * practice or simulation exam. Each answer is stored the moment it is chosen (BR-08),
 * so a reload or another tab opens the same attempt where it was.
 */
export function QuizHandlePage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const attempt = useQuizHandle(id)

  if (attempt.isPending) return <Spin />
  if (attempt.isError) {
    const code = problemOf(attempt.error)?.code
    // Scored already: its result is in Practice Details.
    if (code === 'Attempt.Closed') {
      return (
        <Result
          status="info"
          title={errorMessage(attempt.error)}
          extra={<Button type="primary" onClick={() => navigate(`/practices/${id}`)}>Practice Details</Button>}
        />
      )
    }
    return (
      <Result
        status="warning"
        title={errorMessage(attempt.error)}
        extra={<Button onClick={() => navigate('/practices')}>Practices List</Button>}
      />
    )
  }

  return <Handle attempt={attempt.data} />
}

function Handle({ attempt }: { attempt: QuizHandle }) {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const total = attempt.questions.length
  const [order] = useState(Math.min(Math.max(attempt.currentOrder, 1), total))
  const [answers, setAnswers] = useState<Record<number, number | null>>(() =>
    Object.fromEntries(attempt.questions.map((q) => [q.order, q.chosenOptionId])),
  )
  const answer = useAnswer(attempt.id)
  const question = attempt.questions[order - 1]

  // A change the server refuses: a scored attempt opens its result; anything else is E2.
  const failed = (e: unknown) => {
    const code = problemOf(e)?.code
    if (code === 'Attempt.Closed') {
      message.info(errorMessage(e))
      navigate(`/practices/${attempt.id}`)
      return
    }
    message.error(`Not saved: ${errorMessage(e)}`)
  }

  // UC-24 steps 2 and 3, A1: shown at once, stored right after; a refusal puts it back.
  const choose = (optionId: number) => {
    const before = answers[order]
    setAnswers((a) => ({ ...a, [order]: optionId }))
    answer.mutate(
      { order, optionId },
      {
        onError: (e) => {
          setAnswers((a) => ({ ...a, [order]: before }))
          failed(e)
        },
      },
    )
  }

  return (
    <Flex vertical gap={16}>
      <Flex justify="space-between" align="center" wrap gap={8}>
        <div>
          <Typography.Text type="secondary">{attempt.subjectName}</Typography.Text>
          <Typography.Title level={4} style={{ margin: 0 }}>
            {attempt.title}
          </Typography.Title>
        </div>
        <Space size="large">
          <Typography.Text strong>
            Question {order} of {total}
          </Typography.Text>
          <Clock attempt={attempt} />
        </Space>
      </Flex>

      <QuestionCard attemptId={attempt.id} question={question} chosen={answers[order] ?? null} onChoose={choose} />
    </Flex>
  )
}

function QuestionCard({
  attemptId,
  question,
  chosen,
  onChoose,
}: {
  attemptId: number
  question: HandleQuestion
  chosen: number | null
  onChoose: (optionId: number) => void
}) {
  return (
    <Card
      title={
        <Space>
          <span>Question {question.order}</span>
        </Space>
      }
      extra={<Typography.Text type="secondary">Question ID: {question.questionId}</Typography.Text>}
    >
      <Typography.Paragraph style={{ whiteSpace: 'pre-wrap', fontSize: 16 }}>{question.content}</Typography.Paragraph>
      <Media attemptId={attemptId} question={question} />
      <Radio.Group value={chosen} onChange={(e) => onChoose(e.target.value as number)} style={{ width: '100%' }}>
        <Flex vertical gap={8}>
          {question.options.map((o) => (
            <Radio key={o.id} value={o.id} style={{ alignItems: 'flex-start', fontSize: 15 }}>
              <Typography.Text strong>{o.letter}.</Typography.Text> <span style={{ whiteSpace: 'pre-wrap' }}>{o.content}</span>
            </Radio>
          ))}
        </Flex>
      </Radio.Group>
    </Card>
  )
}

/** The image, audio or video of the question, streamed by the API after it checks the attempt. */
function Media({ attemptId, question }: { attemptId: number; question: HandleQuestion }) {
  const src = mediaUrl(attemptId, question.order)
  if (question.mediaKind === 'image') return <img src={src} alt="" style={{ maxWidth: '100%', maxHeight: 360, marginBottom: 16 }} />
  if (question.mediaKind === 'audio') return <audio src={src} controls style={{ display: 'block', marginBottom: 16 }} />
  if (question.mediaKind === 'video') return <video src={src} controls style={{ maxWidth: '100%', maxHeight: 360, marginBottom: 16 }} />
  return null
}

/**
 * BR-06: time spent on a practice, counting up; time left in a simulation exam, counting
 * down. Read on the server's clock, so a wrong clock on the computer changes nothing.
 */
function Clock({ attempt }: { attempt: QuizHandle }) {
  const [offset] = useState(() => new Date(attempt.serverNow).getTime() - Date.now())
  const [now, setNow] = useState(() => Date.now() + offset)
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now() + offset), 1000)
    return () => clearInterval(timer)
  }, [offset])

  const ms = attempt.endsAt
    ? Math.max(0, new Date(attempt.endsAt).getTime() - now)
    : Math.max(0, now - new Date(attempt.startedAt).getTime())
  const low = attempt.endsAt !== null && ms < 60_000
  return (
    <Tag icon={<ClockCircleOutlined />} color={low ? 'red' : attempt.endsAt ? 'blue' : 'default'} style={{ fontSize: 16, padding: '4px 10px' }}>
      {formatDuration(ms)}
    </Tag>
  )
}
