import { Card, Carousel, Empty, Typography } from 'antd'
import { PublicSider } from '../../layouts/PublicLayout'
import { useSliderList } from '../../hooks/useSliders'

/**
 * Home Page (SRS III 2.1; F#7), only the slider part for now: shown sliders, in their
 * display order. The rest of the page belongs to feature/kiet-public.
 */
export function HomePage() {
  const sliders = useSliderList({ page: 1, pageSize: 10, status: 'Shown' })

  return (
    <PublicSider
      blocks={
        <>
          <Card size="small" title="Featured subjects">
            <Typography.Text type="secondary">Coming with Subjects (feature/kiet-public).</Typography.Text>
          </Card>
          <Card size="small" title="Latest posts">
            <Typography.Text type="secondary">Coming with Blogs (feature/kiet-public).</Typography.Text>
          </Card>
        </>
      }
    >
      {sliders.data?.items.length ? (
        <Carousel autoplay>
          {sliders.data.items.map((s) => (
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
      ) : (
        <Empty description="No slider is shown yet." />
      )}
    </PublicSider>
  )
}
