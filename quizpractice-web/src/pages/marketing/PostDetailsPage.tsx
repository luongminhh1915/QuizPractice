import { useEffect } from 'react'
import { App, Button, Form, Input, Radio, Result, Select, Space, Spin, Switch } from 'antd'
import { useLocation, useNavigate, useParams } from 'react-router'
import { postsApi, type PostInput } from '../../api/postsApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { ImageUpload } from '../../components/ImageUpload'
import { PageHeader } from '../../components/PageHeader'
import { RichTextEditor } from '../../components/RichTextEditor'
import { BRIEF_MAX, NAME_MAX } from '../../constants/fieldLength'
import { usePost, usePostCategories, useSavePost } from '../../hooks/usePosts'

/**
 * Post Details (SRS III 5.3; F#20; UC-36 Add or Edit Post). One form for add and edit
 * (SRS III 0.4): /manage/posts/new opens it empty, /manage/posts/:id filled.
 */
export function PostDetailsPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const params = useParams()
  const id = params.id ? Number(params.id) : undefined
  const isNew = id === undefined

  const [form] = Form.useForm<PostInput>()
  const stored = usePost(id)
  const categories = usePostCategories()
  const save = useSavePost(id)

  // Back to the list with the criteria it had (SRS III 0.3), or to the plain list.
  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/posts'

  useEffect(() => {
    if (stored.data) {
      const { title, categoryId, brief, content, isFeatured, status } = stored.data
      form.setFieldsValue({ title, categoryId, brief, content, isFeatured, status, thumbnail: undefined })
    }
  }, [stored.data, form])

  if (!isNew && stored.isPending) return <Spin />
  if (!isNew && stored.isError) {
    const notFound = problemOf(stored.error)?.status === 404
    return (
      <Result
        status={notFound ? '404' : 'error'}
        title={notFound ? 'This post does not exist.' : errorMessage(stored.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Posts List</Button>}
      />
    )
  }

  // Active categories can be chosen; an inactive one only stays on the post that has it.
  const categoryOptions = (categories.data ?? [])
    .filter((c) => c.status === 'Active' || c.id === stored.data?.categoryId)
    .map((c) => ({ value: c.id, label: c.status === 'Active' ? c.name : `${c.name} (no longer in use)` }))

  const onFinish = (values: PostInput) => {
    save.mutate(
      { input: values, version: stored.data?.version },
      {
        onSuccess: (post) => {
          message.success(isNew ? `Post "${post.title}" was added.` : `Post "${post.title}" was saved.`)
          navigate(backTo)
        },
        onError: (e) => {
          // UC-36 E3: reload the current values and ask for the change again.
          if (problemOf(e)?.code === 'Post.Changed') {
            message.warning(errorMessage(e), 6)
            void stored.refetch()
            return
          }
          // UC-36 E1/E2: keep everything entered and mark the fields to correct.
          if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
        },
      },
    )
  }

  return (
    <>
      <PageHeader title={isNew ? 'New Post' : 'Post Details'} />
      <Form<PostInput>
        form={form}
        layout="vertical"
        initialValues={{ status: 'Hidden', isFeatured: false, content: '' }}
        onFinish={onFinish}
        style={{ maxWidth: 880 }}
      >
        <Form.Item
          label="Thumbnail"
          name="thumbnail"
          rules={isNew ? [{ required: true, message: 'A thumbnail is required for a new post.' }] : []}
          extra="JPG or PNG, up to 2 MB. Shown with the post in lists."
        >
          <ImageUpload currentUrl={stored.data?.thumbnail} width={280} />
        </Form.Item>

        <Form.Item label="Category" name="categoryId" rules={[{ required: true, message: 'Choose a post category.' }]}>
          <Select options={categoryOptions} loading={categories.isPending} placeholder="Choose a category" style={{ maxWidth: 320 }} />
        </Form.Item>

        <Form.Item label="Title" name="title" rules={[{ required: true, whitespace: true }, { max: NAME_MAX }]}>
          <Input />
        </Form.Item>

        <Space size="large" align="start">
          <Form.Item label="Featured" name="isFeatured" valuePropName="checked" extra="Put forward on the Home Page and Blogs List.">
            <Switch />
          </Form.Item>
          <Form.Item label="Status" name="status" extra="Only a shown post reaches the public pages.">
            <Radio.Group optionType="button" options={['Shown', 'Hidden']} />
          </Form.Item>
        </Space>

        <Form.Item
          label="Brief Information"
          name="brief"
          rules={[{ required: true, whitespace: true }, { max: BRIEF_MAX }]}
          extra="Shown under the title in Blogs List."
        >
          <Input.TextArea rows={3} showCount maxLength={BRIEF_MAX} />
        </Form.Item>

        <Form.Item label="Description" name="content">
          <RichTextEditor uploadImage={postsApi.uploadContentImage} />
        </Form.Item>

        <Space>
          <Button type="primary" htmlType="submit" loading={save.isPending}>
            Save
          </Button>
          <Button onClick={() => navigate(backTo)}>Cancel</Button>
        </Space>
      </Form>
    </>
  )
}
