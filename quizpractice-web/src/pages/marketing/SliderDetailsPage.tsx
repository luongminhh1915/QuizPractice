import { useEffect } from 'react'
import { App, Button, Form, Input, Radio, Result, Space, Spin } from 'antd'
import { useLocation, useNavigate, useParams } from 'react-router'
import type { SliderInput } from '../../api/slidersApi'
import { applyFieldErrors, errorMessage, problemOf } from '../../api/client'
import { ImageUpload } from '../../components/ImageUpload'
import { PageHeader } from '../../components/PageHeader'
import { BRIEF_MAX, NAME_MAX, URL_MAX } from '../../constants/fieldLength'
import { useSaveSlider, useSlider } from '../../hooks/useSliders'

/**
 * Slider Details (SRS III 5.5; F#22; UC-39 Add or Edit Slider). One form for add and
 * edit (SRS III 0.4): /manage/sliders/new opens it empty, /manage/sliders/:id filled.
 * The sample detail screen: copy its shape for every other form.
 */
export function SliderDetailsPage() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()
  const params = useParams()
  const id = params.id ? Number(params.id) : undefined
  const isNew = id === undefined

  const [form] = Form.useForm<SliderInput>()
  const stored = useSlider(id)
  const save = useSaveSlider(id)

  // Back to the list with the criteria it had (SRS III 0.3), or to the plain list.
  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/sliders'

  useEffect(() => {
    if (stored.data) {
      const { title, backlink, status, notes } = stored.data
      form.setFieldsValue({ title, backlink: backlink ?? undefined, status, notes: notes ?? undefined })
    }
  }, [stored.data, form])

  if (!isNew && stored.isPending) return <Spin />
  if (!isNew && stored.isError) {
    const notFound = problemOf(stored.error)?.status === 404
    return (
      <Result
        status={notFound ? '404' : 'error'}
        title={notFound ? 'This slider does not exist.' : errorMessage(stored.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Sliders List</Button>}
      />
    )
  }

  const onFinish = (values: SliderInput) => {
    save.mutate(values, {
      onSuccess: (slider) => {
        message.success(isNew ? `Slider "${slider.title}" was added.` : `Slider "${slider.title}" was saved.`)
        navigate(backTo)
      },
      // UC-39 E1/E2: keep everything entered and mark the fields to correct.
      onError: (e) => {
        if (!applyFieldErrors(form, e)) message.error(errorMessage(e))
      },
    })
  }

  return (
    <>
      <PageHeader title={isNew ? 'New Slider' : 'Slider Details'} />
      <Form<SliderInput>
        form={form}
        layout="vertical"
        initialValues={{ status: 'Hidden' }}
        onFinish={onFinish}
        style={{ maxWidth: 640 }}
      >
        <Form.Item
          label="Image"
          name="image"
          rules={isNew ? [{ required: true, message: 'An image is required for a new slider.' }] : []}
          extra="JPG or PNG, up to 2 MB. Shown on the Home Page at this ratio."
        >
          <ImageUpload currentUrl={stored.data?.image} />
        </Form.Item>

        <Form.Item label="Title" name="title" rules={[{ required: true, whitespace: true }, { max: NAME_MAX }]}>
          <Input />
        </Form.Item>

        <Form.Item
          label="Backlink"
          name="backlink"
          rules={[
            { max: URL_MAX },
            { type: 'url', message: 'The backlink must be a full https:// address.' },
            { pattern: /^https:\/\//, message: 'The backlink must be a full https:// address.' },
          ]}
        >
          <Input placeholder="https://" />
        </Form.Item>

        <Form.Item label="Status" name="status" extra="Only a shown slider appears on the Home Page.">
          <Radio.Group optionType="button" options={['Shown', 'Hidden']} />
        </Form.Item>

        <Form.Item label="Notes" name="notes" rules={[{ max: BRIEF_MAX }]}>
          <Input.TextArea rows={3} showCount maxLength={BRIEF_MAX} />
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
