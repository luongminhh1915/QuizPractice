import { useState } from 'react'
import { Button, Card, Flex, Result, Space, Spin, Tag, Typography } from 'antd'
import { CheckCircleFilled, CloseCircleFilled, FlagFilled, LeftOutlined, RightOutlined } from '@ant-design/icons'
import { useNavigate, useParams } from 'react-router'
import { mediaUrl, type QuizReview, type ReviewQuestion } from '../../api/attemptApi'
import { errorMessage } from '../../api/client'
import { useQuizReview } from '../../hooks/useAttempt'

/**
 * Quiz Review (SRS III 4.2; F#18; UC-31 to UC-33): a scored attempt question by question,
 * with the answer given, the correct answer and whether it was right. Opened from
 * Practice Review on Practice Details (UC-21), right after scoring too (A1, A2).
 */
export function QuizReviewPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const review = useQuizReview(id)

  if (review.isPending) return <Spin />
  if (review.isError) {
    return (
      <Result
        status="warning"
        title={errorMessage(review.error)}
        extra={<Button onClick={() => navigate('/practices')}>Practices List</Button>}
      />
    )
  }

  return <Review review={review.data} />
}

function Review({ review }: { review: QuizReview }) {
  const navigate = useNavigate()
  const total = review.questions.length
  const [order, setOrder] = useState(1)
  const question = review.questions[order - 1]
  const r = review.result

  return (
    <Flex vertical gap={16}>
      <Flex justify="space-between" align="center" wrap gap={8}>
        <div>
          <Typography.Text type="secondary">{review.subjectName}</Typography.Text>
          <Typography.Title level={4} style={{ margin: 0 }}>
            {review.title}
          </Typography.Title>
        </div>
        {total > 0 && (
          <Typography.Text strong>
            Question {order} of {total}
          </Typography.Text>
        )}
      </Flex>

      {/* Result Summary: judged against the pass rate stored with the attempt (BR-21). */}
      <Card size="small">
        <Space wrap size="large">
          <span>
            <Typography.Text strong>
              {r.correct} / {r.questionCount}
            </Typography.Text>{' '}
            correct ({r.score}%)
          </span>
          <span>
            <Tag color={r.passed ? 'green' : 'red'}>{r.passed ? 'Passed' : 'Failed'}</Tag>
            <Typography.Text type="secondary">pass rate {r.passRate}%</Typography.Text>
          </span>
        </Space>
      </Card>

      {/* E1: no question detail stored; the figures above are all there is. */}
      {total === 0 ? (
        <Result status="info" title="The question detail of this attempt is not available." />
      ) : (
        <ReviewCard attemptId={review.id} question={question} />
      )}

      <Flex justify="space-between" align="center" wrap gap={8}>
        <Space wrap>
          <Button onClick={() => navigate(`/practices/${review.id}`)}>Practice Details</Button>
        </Space>
        <Space>
          {order > 1 && (
            <Button icon={<LeftOutlined />} onClick={() => setOrder(order - 1)}>
              Previous
            </Button>
          )}
          {order < total && (
            <Button type="primary" onClick={() => setOrder(order + 1)}>
              Next <RightOutlined />
            </Button>
          )}
        </Space>
      </Flex>
    </Flex>
  )
}

function ReviewCard({ attemptId, question }: { attemptId: number; question: ReviewQuestion }) {
  const src = mediaUrl(attemptId, question.order)
  return (
    <Card
      title={
        <Space>
          <span>Question {question.order}</span>
          {question.isCorrect ? (
            <Tag icon={<CheckCircleFilled />} color="green">
              Correct
            </Tag>
          ) : (
            <Tag icon={<CloseCircleFilled />} color="red">
              {question.chosenOptionId === null ? 'Not answered' : 'Incorrect'}
            </Tag>
          )}
          {question.isMarked && (
            <Tag icon={<FlagFilled />} color="orange">
              Marked
            </Tag>
          )}
        </Space>
      }
      extra={<Typography.Text type="secondary">Question ID: {question.questionId}</Typography.Text>}
    >
      <Typography.Paragraph style={{ whiteSpace: 'pre-wrap', fontSize: 16 }}>{question.content}</Typography.Paragraph>
      {question.mediaKind === 'image' && <img src={src} alt="" style={{ maxWidth: '100%', maxHeight: 360, marginBottom: 16 }} />}
      {question.mediaKind === 'audio' && <audio src={src} controls style={{ display: 'block', marginBottom: 16 }} />}
      {question.mediaKind === 'video' && <video src={src} controls style={{ maxWidth: '100%', maxHeight: 360, marginBottom: 16 }} />}
      <Flex vertical gap={8}>
        {question.options.map((o) => {
          const chosen = o.id === question.chosenOptionId
          const border = o.isCorrect ? '#52c41a' : chosen ? '#ff4d4f' : '#f0f0f0'
          const background = o.isCorrect ? '#f6ffed' : chosen ? '#fff2f0' : undefined
          return (
            <Flex
              key={o.id}
              justify="space-between"
              align="center"
              gap={8}
              style={{ border: `1px solid ${border}`, background, borderRadius: 6, padding: '8px 12px', fontSize: 15 }}
            >
              <span style={{ whiteSpace: 'pre-wrap' }}>
                <Typography.Text strong>{o.letter}.</Typography.Text> {o.content}
              </span>
              <Space size={4}>
                {chosen && <Tag color={o.isCorrect ? 'green' : 'red'}>YOUR ANSWER</Tag>}
                {o.isCorrect && <CheckCircleFilled style={{ color: '#52c41a' }} />}
              </Space>
            </Flex>
          )
        })}
      </Flex>
    </Card>
  )
}
