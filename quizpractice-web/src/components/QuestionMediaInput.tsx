import { useEffect, useRef, useState } from 'react'
import { App, Button, Flex, Image, Typography, Upload } from 'antd'
import { UploadOutlined } from '@ant-design/icons'
import { QUESTION_MEDIA } from '../constants/fieldLength'

export type MediaKind = 'image' | 'audio' | 'video'

interface QuestionMediaInputProps {
  /** New file chosen (Form.Item gives value/onChange). */
  value?: File
  onChange?: (file: File | undefined) => void
  /** The stored media, shown until a new file is chosen or it is removed. */
  current?: { url: string; kind: MediaKind }
  /** The stored media is to be dropped on Save. */
  removed: boolean
  onRemovedChange: (removed: boolean) => void
}

const kindOf = (file: File) => QUESTION_MEDIA.find((m) => (m.types as readonly string[]).includes(file.type))

/** Plays an image, an audio or a video in place. */
export function MediaPreview({ url, kind }: { url: string; kind: MediaKind }) {
  if (kind === 'image') return <Image src={url} width={320} style={{ borderRadius: 6 }} />
  if (kind === 'audio') return <audio src={url} controls style={{ width: 320 }} />
  return <video src={url} controls width={360} style={{ borderRadius: 6 }} />
}

/**
 * Media field of Question Details (SRS III 7.2; UC-53 step 4, E3): an image, an MP3 or
 * an MP4, previewed in place, with Replace and Remove. The file is only kept in the form
 * and sent with Save. Type and size are checked here to answer at once; the API checks
 * them again, including the real file signature.
 */
export function QuestionMediaInput({ value, onChange, current, removed, onRemovedChange }: QuestionMediaInputProps) {
  const { message } = App.useApp()
  const [preview, setPreview] = useState<{ file: File; url: string; kind: MediaKind }>()
  const lastUrl = useRef<string>(undefined)

  useEffect(() => () => {
    if (lastUrl.current) URL.revokeObjectURL(lastUrl.current)
  }, [])

  const choose = (file: File | undefined) => {
    if (lastUrl.current) URL.revokeObjectURL(lastUrl.current)
    const type = file && kindOf(file)
    const url = file ? URL.createObjectURL(file) : undefined
    lastUrl.current = url
    setPreview(file && url && type ? { file, url, kind: type.kind } : undefined)
    onChange?.(file)
  }

  // The preview only counts while its file is still the field value, so a form reset
  // falls back to the stored media by itself.
  const fresh = value && preview?.file === value ? preview : undefined
  const shown = fresh ?? (removed ? undefined : current)

  const picker = (label: string) => (
    <Upload
      accept={QUESTION_MEDIA.flatMap((m) => m.types).join(',')}
      showUploadList={false}
      beforeUpload={(file) => {
        const type = kindOf(file)
        if (!type) {
          message.error('The media must be a JPG, PNG or WebP image, an MP3 or an MP4 file.')
        } else if (file.size > type.maxBytes) {
          message.error(`The ${type.kind} must be ${type.maxBytes / (1024 * 1024)} MB or smaller.`)
        } else {
          choose(file)
        }
        return false // never upload on its own; the form sends it on Save
      }}
    >
      <Button icon={<UploadOutlined />}>{label}</Button>
    </Upload>
  )

  return (
    <Flex vertical gap={8} align="start">
      {shown ? (
        <MediaPreview url={shown.url} kind={shown.kind} />
      ) : (
        <Typography.Text type="secondary">{removed ? 'The media will be removed on Save.' : 'No media.'}</Typography.Text>
      )}
      <Flex gap={8}>
        {picker(shown ? 'Replace' : 'Upload media')}
        {shown && (
          <Button
            onClick={() => {
              if (fresh) choose(undefined)
              else onRemovedChange(true)
            }}
          >
            Remove
          </Button>
        )}
        {!shown && removed && <Button onClick={() => onRemovedChange(false)}>Keep the stored media</Button>}
      </Flex>
    </Flex>
  )
}
