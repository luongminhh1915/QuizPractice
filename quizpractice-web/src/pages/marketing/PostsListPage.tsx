import { App, Button, Input, Select, Space, Tag, Typography } from 'antd'
import { PlusOutlined, StarFilled } from '@ant-design/icons'
import { Link, useLocation } from 'react-router'
import type { PostSummary } from '../../api/postsApi'
import type { VisibilityStatus } from '../../api/types'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useListParams } from '../../hooks/useListParams'
import { useChangePostStatus, usePostCategories, usePostList } from '../../hooks/usePosts'

/** Posts List (SRS III 5.2; PDF II.5; UC-35 View Posts List, UC-37 Show or Hide Post). */
export function PostsListPage() {
  const { message } = App.useApp()
  const location = useLocation()
  const list = useListParams()
  const status = list.get('status') as VisibilityStatus | undefined
  const categoryParam = list.get('categoryId')
  const categoryId = categoryParam ? Number(categoryParam) : undefined
  const query = usePostList({ ...list.criteria, status, categoryId })
  const categories = usePostCategories()
  const changeStatus = useChangePostStatus()

  // Detail screens return to this list with the same criteria (SRS III 0.3).
  const back = { state: { back: location.pathname + location.search } }

  const toggle = (post: PostSummary) => {
    const next: VisibilityStatus = post.status === 'Shown' ? 'Hidden' : 'Shown'
    changeStatus.mutate(
      { id: post.id, status: next },
      {
        // UC-37 A1 and E1: a post already in that state (another user got there first)
        // is left as it is; the list reloads either way.
        onSuccess: () => message.success(`"${post.title}" is now ${next.toLowerCase()}.`),
        onError: (e) => message.error(errorMessage(e)),
      },
    )
  }

  const columns: DataTableColumn<PostSummary>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 80 },
    {
      title: 'Thumbnail',
      key: 'thumbnail',
      width: 110,
      render: (p) => (
        <img src={p.thumbnail} alt="" style={{ width: 88, height: 50, objectFit: 'cover', borderRadius: 4 }} />
      ),
    },
    { title: 'Title', key: 'title', sortable: true },
    { title: 'Category', key: 'category', sortable: true, width: 160 },
    {
      title: 'Featured',
      key: 'featured',
      sortable: true,
      width: 120,
      render: (p) =>
        p.isFeatured ? (
          <Tag icon={<StarFilled />} color="gold">
            Featured
          </Tag>
        ) : (
          <Typography.Text type="secondary">—</Typography.Text>
        ),
    },
    { title: 'Status', key: 'status', sortable: true, width: 110, render: (p) => <StatusTag status={p.status} /> },
    {
      title: 'Action',
      key: 'action',
      width: 150,
      render: (p) => (
        <Space>
          <Link to={`/manage/posts/${p.id}`} {...back}>
            <Button size="small">Edit</Button>
          </Link>
          <Button
            size="small"
            danger={p.status === 'Shown'}
            loading={changeStatus.isPending && changeStatus.variables?.id === p.id}
            onClick={() => toggle(p)}
          >
            {p.status === 'Shown' ? 'Hide' : 'Show'}
          </Button>
        </Space>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Posts List"
        action={
          <Link to="/manage/posts/new" {...back}>
            <Button type="primary" icon={<PlusOutlined />}>
              Add New Post
            </Button>
          </Link>
        }
      />

      <Space wrap style={{ marginBottom: 16 }}>
        {/* key resets the box when the criteria are cleared */}
        <Input.Search
          key={list.criteria.search ?? ''}
          defaultValue={list.criteria.search}
          placeholder="Search title"
          allowClear
          onSearch={(value) => list.set({ search: value.trim() })}
          style={{ width: 300 }}
        />
        <Select
          aria-label="Category"
          value={categoryId ?? 'All'}
          onChange={(value) => list.set({ categoryId: value === 'All' ? undefined : value })}
          loading={categories.isPending}
          options={[
            { value: 'All', label: 'All categories' },
            ...(categories.data ?? []).map((c) => ({ value: c.id, label: c.name })),
          ]}
          style={{ width: 180 }}
        />
        <Select
          aria-label="Status"
          value={status ?? 'All'}
          onChange={(value) => list.set({ status: value === 'All' ? undefined : value })}
          options={[{ value: 'All', label: 'All statuses' }, { value: 'Shown' }, { value: 'Hidden' }]}
          style={{ width: 140 }}
        />
      </Space>

      {query.isError && <Typography.Paragraph type="danger">{errorMessage(query.error)}</Typography.Paragraph>}

      <DataTable
        columns={columns}
        page={query.data}
        loading={query.isFetching}
        criteria={list.criteria}
        onChange={list.set}
        hasCriteria={list.hasCriteria}
        onClearCriteria={list.clear}
      />
    </>
  )
}
