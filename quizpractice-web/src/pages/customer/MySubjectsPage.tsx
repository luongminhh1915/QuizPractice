import { Button, Card, Col, Flex, Result, Row, Spin, Typography } from 'antd'
import { useNavigate } from 'react-router'
import { errorMessage } from '../../api/client'
import { useMySubjects } from '../../hooks/useMySubjects'

/**
 * My Subjects (SRS III 3.6; UC-71): the subjects a Paid registration covers today (BR-05).
 * A subject opens its lessons, where the lesson quizzes are taken.
 */
export function MySubjectsPage() {
  const navigate = useNavigate()
  const subjects = useMySubjects()

  if (subjects.isPending) return <Spin />
  if (subjects.isError) return <Result status="warning" title={errorMessage(subjects.error)} />
  if (subjects.data.length === 0) {
    return (
      <Result
        status="info"
        title="You have no subject to study yet."
        subTitle="The lessons of a subject open once its registration is paid and still valid."
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
      <Typography.Title level={3} style={{ margin: 0 }}>
        My Subjects
      </Typography.Title>
      <Row gutter={[16, 16]}>
        {subjects.data.map((s) => (
          <Col key={s.id} xs={24} sm={12} lg={8}>
            <Card
              hoverable
              cover={<img alt="" src={s.thumbnail} style={{ height: 150, objectFit: 'cover' }} />}
              onClick={() => navigate(`/my-subjects/${s.id}`)}
            >
              <Card.Meta
                title={s.name}
                description={
                  <Typography.Paragraph type="secondary" ellipsis={{ rows: 2 }} style={{ margin: 0 }}>
                    {s.brief}
                  </Typography.Paragraph>
                }
              />
            </Card>
          </Col>
        ))}
      </Row>
    </Flex>
  )
}
