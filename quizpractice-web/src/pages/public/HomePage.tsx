import { Carousel, Col, Flex, Result, Row, Spin, Typography } from 'antd'
import { errorMessage } from '../../api/client'
import { PostCardView, SubjectCardView } from '../../components/public/Cards'
import { ContactsBlock, LatestPostsBlock } from '../../components/public/SiderBlocks'
import { PublicSider } from '../../layouts/PublicLayout'
import { useHome } from '../../hooks/usePublic'
import { useShownSliders } from '../../hooks/useSliders'

/**
 * Home Page (SRS III 2.1; F#7; UC-09): shown sliders, hot posts and featured subjects,
 * with the latest posts and the contacts in the sider. A block with nothing to show is
 * left out and the rest of the page stays (A1).
 */
export function HomePage() {
  const sliders = useShownSliders()
  const home = useHome()

  return (
    <PublicSider
      blocks={
        <>
          <LatestPostsBlock posts={home.data?.latestPosts} />
          <ContactsBlock />
        </>
      }
    >
      <Flex vertical gap={32}>
        {!!sliders.data?.length && (
          <Carousel autoplay arrows>
            {sliders.data.map((s) => (
              <a key={s.id} href={s.backlink ?? undefined} target="_blank" rel="noopener noreferrer">
                <div style={{ position: 'relative' }}>
                  <img src={s.image} alt={s.title} style={{ width: '100%', height: 320, objectFit: 'cover', borderRadius: 8 }} />
                  <Typography.Title
                    level={3}
                    style={{ position: 'absolute', left: 24, bottom: 16, margin: 0, color: '#fff', textShadow: '0 1px 4px #0008' }}
                  >
                    {s.title}
                  </Typography.Title>
                </div>
              </a>
            ))}
          </Carousel>
        )}

        {home.isPending && <Spin style={{ display: 'block', padding: 32 }} />}
        {/* E1: the page says so; the sliders above may still be there. */}
        {home.isError && <Result status="warning" title={errorMessage(home.error)} />}

        {!!home.data?.hotPosts.length && (
          <section>
            <Typography.Title level={4}>Hot Posts</Typography.Title>
            <Row gutter={[16, 16]}>
              {home.data.hotPosts.map((p) => (
                <Col key={p.id} xs={24} sm={12} lg={8}>
                  <PostCardView post={p} showBrief={false} />
                </Col>
              ))}
            </Row>
          </section>
        )}

        {!!home.data?.featuredSubjects.length && (
          <section>
            <Typography.Title level={4}>Featured Subjects</Typography.Title>
            <Row gutter={[16, 16]}>
              {home.data.featuredSubjects.map((s) => (
                <Col key={s.id} xs={24} sm={12} lg={8}>
                  <SubjectCardView subject={s} />
                </Col>
              ))}
            </Row>
          </section>
        )}
      </Flex>
    </PublicSider>
  )
}
