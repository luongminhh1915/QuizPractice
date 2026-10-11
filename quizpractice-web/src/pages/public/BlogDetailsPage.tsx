import { useEffect } from 'react'
import { App, Flex, Result, Spin, Tag, Typography } from 'antd'
import dayjs from 'dayjs'
import { Link, useNavigate, useParams } from 'react-router'
import { errorMessage, problemOf } from '../../api/client'
import { CategoriesBlock, ContactsBlock, LatestPostsBlock, SearchBlock } from '../../components/public/SiderBlocks'
import { useBlogPost, useBlogSider } from '../../hooks/usePublic'
import { PublicSider } from '../../layouts/PublicLayout'

/**
 * Blog Details (SRS III 2.3; F#9; UC-11). The sider is the one of Blogs List: a search
 * or a category there opens Blogs List with it.
 */
export function BlogDetailsPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const id = Number(useParams().id)
  const post = useBlogPost(id)
  const sider = useBlogSider()
  const gone = problemOf(post.error)?.status === 404

  // E1: removed or hidden since it was linked; back to the list with a message.
  useEffect(() => {
    if (gone) {
      message.warning('This post is not available any more.')
      navigate('/blogs', { replace: true })
    }
  }, [gone, message, navigate])

  return (
    <PublicSider
      blocks={
        <>
          <SearchBlock placeholder="Search posts by title" onSearch={(s) => navigate(s ? `/blogs?search=${encodeURIComponent(s)}` : '/blogs')} />
          <CategoriesBlock
            categories={sider.data?.categories ?? []}
            selected={post.data?.categoryId}
            onSelect={(c) => navigate(c ? `/blogs?categoryId=${c}` : '/blogs')}
          />
          <LatestPostsBlock posts={sider.data?.latestPosts} />
          <ContactsBlock />
        </>
      }
    >
      {post.isPending && <Spin style={{ display: 'block', padding: 48 }} />}
      {post.isError && !gone && <Result status="warning" title={errorMessage(post.error)} />}
      {post.data && (
        <article>
          <Typography.Title level={2} style={{ marginBottom: 8 }}>
            {post.data.title}
          </Typography.Title>
          <Flex gap={12} align="center" wrap style={{ marginBottom: 16 }}>
            <Link to={`/blogs?categoryId=${post.data.categoryId}`}>
              <Tag color="blue">{post.data.category}</Tag>
            </Link>
            <Typography.Text type="secondary">
              By {post.data.author} · Updated {dayjs(post.data.updatedAt).format('DD/MM/YYYY HH:mm')}
            </Typography.Text>
          </Flex>
          <img src={post.data.thumbnail} alt={post.data.title} style={{ width: '100%', maxHeight: 360, objectFit: 'cover', borderRadius: 8 }} />
          <Typography.Paragraph strong style={{ marginTop: 16 }}>
            {post.data.brief}
          </Typography.Paragraph>
          {/* The HTML was cleaned by the API's sanitizer when the post was saved (UC-36). */}
          <div className="post-content" dangerouslySetInnerHTML={{ __html: post.data.content }} />
        </article>
      )}
    </PublicSider>
  )
}
