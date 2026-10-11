import type { ReactNode } from 'react'
import { Card, Flex, Tag, Typography } from 'antd'
import dayjs from 'dayjs'
import { Link } from 'react-router'
import type { PostCard, SubjectCard } from '../../api/publicApi'
import { PriceTag } from './PriceTag'

/** One shown post: thumbnail, title, category, date and brief (SRS III 2.1, 2.2). The card opens Blog Details. */
export function PostCardView({ post, showBrief = true }: { post: PostCard; showBrief?: boolean }) {
  return (
    <Link to={`/blogs/${post.id}`}>
      <Card
        hoverable
        cover={<img src={post.thumbnail} alt={post.title} style={{ height: 160, objectFit: 'cover' }} />}
        styles={{ body: { padding: 16 } }}
      >
        <Flex vertical gap={6}>
          <Flex justify="space-between" align="center">
            <Tag>{post.category}</Tag>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {dayjs(post.updatedAt).format('DD/MM/YYYY')}
            </Typography.Text>
          </Flex>
          <Typography.Title level={5} style={{ margin: 0 }} ellipsis={{ rows: 2 }}>
            {post.title}
          </Typography.Title>
          {showBrief && (
            <Typography.Paragraph type="secondary" ellipsis={{ rows: 2 }} style={{ margin: 0 }}>
              {post.brief}
            </Typography.Paragraph>
          )}
        </Flex>
      </Card>
    </Link>
  )
}

/**
 * One published subject: thumbnail, title, tag line, and the price of its cheapest
 * package when withPrice (SRS III 2.1, 2.4). The card opens Subject Details; action is
 * the Register button of Subjects List, outside the link.
 */
export function SubjectCardView({ subject, withPrice = false, action }: { subject: SubjectCard; withPrice?: boolean; action?: ReactNode }) {
  return (
    <Card
      hoverable
      cover={
        <Link to={`/subjects/${subject.id}`}>
          <img src={subject.thumbnail} alt={subject.name} style={{ width: '100%', height: 150, objectFit: 'cover' }} />
        </Link>
      }
      styles={{ body: { padding: 16 } }}
    >
      <Flex vertical gap={6}>
        <Link to={`/subjects/${subject.id}`}>
          <Typography.Title level={5} style={{ margin: 0 }} ellipsis={{ rows: 2 }}>
            {subject.name}
          </Typography.Title>
        </Link>
        {subject.tagline && (
          <Typography.Paragraph type="secondary" ellipsis={{ rows: 2 }} style={{ margin: 0 }}>
            {subject.tagline}
          </Typography.Paragraph>
        )}
        {withPrice && <PriceTag price={subject.lowestPrice} />}
        {action}
      </Flex>
    </Card>
  )
}
