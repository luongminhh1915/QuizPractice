import { Button, Col, Empty, Pagination, Result, Row, Spin, Typography } from 'antd'
import { errorMessage } from '../../api/client'
import { PostCardView } from '../../components/public/Cards'
import { CriteriaBar } from '../../components/public/CriteriaBar'
import { CategoriesBlock, ContactsBlock, LatestPostsBlock, SearchBlock } from '../../components/public/SiderBlocks'
import { useListParams } from '../../hooks/useListParams'
import { useBlogs, useBlogSider } from '../../hooks/usePublic'
import { PublicSider } from '../../layouts/PublicLayout'

/**
 * Blogs List (SRS III 2.2; F#8; UC-10): shown posts, most recently updated first, ten a
 * page. The search text and the category sit in the address, so Back from a post
 * returns to the same list (A2).
 */
export function BlogsListPage() {
  const { criteria, get, set, clear } = useListParams()
  const categoryId = get('categoryId') ? Number(get('categoryId')) : undefined
  const blogs = useBlogs({ page: criteria.page, pageSize: criteria.pageSize, search: criteria.search, categoryId })
  const sider = useBlogSider()
  const category = sider.data?.categories.find((c) => c.id === categoryId)

  const applied = [
    ...(criteria.search ? [{ label: `Search: ${criteria.search}`, onRemove: () => set({ search: undefined }) }] : []),
    ...(categoryId ? [{ label: `Category: ${category?.name ?? categoryId}`, onRemove: () => set({ categoryId: undefined }) }] : []),
  ]

  return (
    <PublicSider
      blocks={
        <>
          <SearchBlock placeholder="Search posts by title" value={criteria.search} onSearch={(search) => set({ search })} />
          <CategoriesBlock categories={sider.data?.categories ?? []} selected={categoryId} onSelect={(id) => set({ categoryId: id })} />
          <LatestPostsBlock posts={sider.data?.latestPosts} />
          <ContactsBlock />
        </>
      }
    >
      <Typography.Title level={3}>Blogs</Typography.Title>
      <CriteriaBar total={blogs.data?.totalCount} noun="post" criteria={applied} onClear={clear} />

      {blogs.isPending && <Spin style={{ display: 'block', padding: 48 }} />}
      {blogs.isError && <Result status="warning" title={errorMessage(blogs.error)} />}

      {/* E1: nothing matches; the criteria stay and can be cleared. */}
      {blogs.data && blogs.data.items.length === 0 && (
        <Empty description={applied.length ? 'No post matches these criteria.' : 'No post has been published yet.'}>
          {!!applied.length && <Button onClick={clear}>Clear the criteria</Button>}
        </Empty>
      )}

      {!!blogs.data?.items.length && (
        <>
          <Row gutter={[16, 16]}>
            {blogs.data.items.map((p) => (
              <Col key={p.id} xs={24} sm={12}>
                <PostCardView post={p} />
              </Col>
            ))}
          </Row>
          <Pagination
            style={{ marginTop: 24, justifyContent: 'center' }}
            current={blogs.data.pageNumber}
            pageSize={blogs.data.pageSize}
            total={blogs.data.totalCount}
            showSizeChanger={false}
            hideOnSinglePage
            onChange={(page) => set({ page })}
          />
        </>
      )}
    </PublicSider>
  )
}
