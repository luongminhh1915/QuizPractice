import { useEffect } from 'react'
import { App, Card, Col, Flex, Result, Row, Spin, Tag, Typography } from 'antd'
import { Link, useNavigate, useParams } from 'react-router'
import { errorMessage, problemOf } from '../../api/client'
import { PriceTag } from '../../components/public/PriceTag'
import { CategoriesBlock, ContactsBlock, FeaturedSubjectsBlock, SearchBlock } from '../../components/public/SiderBlocks'
import { useSubject, useSubjectSider } from '../../hooks/usePublic'
import { PublicSider } from '../../layouts/PublicLayout'

/**
 * Subject Details (SRS III 2.5; F#11; UC-13). The sider is the one of Subjects List: a
 * search or a category there opens Subjects List with it.
 */
export function SubjectDetailsPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const id = Number(useParams().id)
  const subject = useSubject(id)
  const sider = useSubjectSider()
  const gone = problemOf(subject.error)?.status === 404

  // E1: no longer published; back to the list with a message.
  useEffect(() => {
    if (gone) {
      message.warning('This subject is not available any more.')
      navigate('/subjects', { replace: true })
    }
  }, [gone, message, navigate])

  const s = subject.data
  return (
    <PublicSider
      blocks={
        <>
          <SearchBlock
            placeholder="Search subjects by name"
            onSearch={(t) => navigate(t ? `/subjects?search=${encodeURIComponent(t)}` : '/subjects')}
          />
          <CategoriesBlock
            categories={sider.data?.categories ?? []}
            selected={s?.categoryId}
            onSelect={(c) => navigate(c ? `/subjects?categoryId=${c}` : '/subjects')}
          />
          <FeaturedSubjectsBlock subjects={sider.data?.featuredSubjects} />
          <ContactsBlock />
        </>
      }
    >
      {subject.isPending && <Spin style={{ display: 'block', padding: 48 }} />}
      {subject.isError && !gone && <Result status="warning" title={errorMessage(subject.error)} />}
      {s && (
        <Flex vertical gap={24}>
          <div>
            <Typography.Title level={2} style={{ marginBottom: 4 }}>
              {s.name}
            </Typography.Title>
            {s.tagline && <Typography.Text type="secondary">{s.tagline}</Typography.Text>}
          </div>
          <Row gutter={[24, 24]}>
            <Col xs={24} md={14}>
              <img src={s.thumbnail} alt={s.name} style={{ width: '100%', borderRadius: 8, objectFit: 'cover' }} />
            </Col>
            <Col xs={24} md={10}>
              <Card>
                <Flex vertical gap={12}>
                  <Link to={`/subjects?categoryId=${s.categoryId}`}>
                    <Tag color="blue">{s.category}</Tag>
                  </Link>
                  <Typography.Paragraph style={{ margin: 0 }}>{s.brief}</Typography.Paragraph>
                  <PriceTag price={s.lowestPrice} />
                  {s.lowestPrice && (
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      Starting price ({s.lowestPrice.packageName}). The other packages are offered when you register.
                    </Typography.Text>
                  )}
                </Flex>
              </Card>
            </Col>
          </Row>
          <section>
            <Typography.Title level={4}>Description</Typography.Title>
            {/* HTML from Subject Details (6.3), cleaned by the API's sanitizer before it is sent. */}
            <div className="post-content" dangerouslySetInnerHTML={{ __html: s.description }} />
          </section>
        </Flex>
      )}
    </PublicSider>
  )
}
