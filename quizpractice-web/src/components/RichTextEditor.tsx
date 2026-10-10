import { useEffect, useState } from 'react'
import { App, Button, Divider, Flex, Input, Modal, Tooltip, Upload } from 'antd'
import {
  BoldOutlined,
  CodeOutlined,
  ItalicOutlined,
  LinkOutlined,
  OrderedListOutlined,
  PictureOutlined,
  RedoOutlined,
  StrikethroughOutlined,
  UnderlineOutlined,
  UndoOutlined,
  UnorderedListOutlined,
} from '@ant-design/icons'
import { EditorContent, useEditor, useEditorState, type Editor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Image from '@tiptap/extension-image'
import { errorMessage } from '../api/client'
import { IMAGE_MAX_BYTES, IMAGE_TYPES } from '../constants/fieldLength'

interface RichTextEditorProps {
  /** HTML (Form.Item gives value/onChange). */
  value?: string
  onChange?: (html: string) => void
  /** Uploads an image for the body and answers its address. Without it there is no image button. */
  uploadImage?: (file: File) => Promise<string>
  minHeight?: number
}

/**
 * Text editor of SRS III 5.3 ("headings, lists, links and images"), on TipTap. It only
 * offers what the API sanitizer keeps (HtmlContentSanitizer): H2–H4, bold, italic,
 * underline, strike, lists, quote, code block, links, images from our own uploads.
 * The API cleans the HTML again before storing it; this is never the safety net.
 */
export function RichTextEditor({ value, onChange, uploadImage, minHeight = 260 }: RichTextEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [2, 3, 4] },
        // The sanitizer drops target and rel; links open in the same tab.
        link: { openOnClick: false, autolink: true, defaultProtocol: 'https', HTMLAttributes: { target: null, rel: null } },
      }),
      Image.configure({ allowBase64: false }),
    ],
    content: value ?? '',
    onUpdate: ({ editor }) => onChange?.(editor.isEmpty ? '' : editor.getHTML()),
  })

  // A value set from outside (the stored post loads, the form resets) replaces the text.
  useEffect(() => {
    const current = editor.isEmpty ? '' : editor.getHTML()
    if ((value ?? '') !== current) editor.commands.setContent(value ?? '', { emitUpdate: false })
  }, [value, editor])

  return (
    <div style={{ border: '1px solid #d9d9d9', borderRadius: 6, background: '#fff' }}>
      <Toolbar editor={editor} uploadImage={uploadImage} />
      <EditorContent editor={editor} className="rich-text" style={{ minHeight, padding: '4px 12px' }} />
    </div>
  )
}

function Toolbar({ editor, uploadImage }: { editor: Editor; uploadImage?: (file: File) => Promise<string> }) {
  const { message } = App.useApp()
  const [uploading, setUploading] = useState(false)
  const [linkOpen, setLinkOpen] = useState(false)
  const [href, setHref] = useState('')

  // Re-render the buttons only when one of these changes, not on every keystroke.
  const state = useEditorState({
    editor,
    selector: ({ editor: e }) => ({
      bold: e.isActive('bold'),
      italic: e.isActive('italic'),
      underline: e.isActive('underline'),
      strike: e.isActive('strike'),
      h2: e.isActive('heading', { level: 2 }),
      h3: e.isActive('heading', { level: 3 }),
      h4: e.isActive('heading', { level: 4 }),
      bullet: e.isActive('bulletList'),
      ordered: e.isActive('orderedList'),
      quote: e.isActive('blockquote'),
      code: e.isActive('codeBlock'),
      link: e.isActive('link'),
      canUndo: e.can().undo(),
      canRedo: e.can().redo(),
    }),
  })

  const chain = () => editor.chain().focus()

  const button = (title: string, active: boolean, onClick: () => void, content: React.ReactNode, disabled = false) => (
    <Tooltip title={title} key={title}>
      <Button
        size="small"
        type={active ? 'primary' : 'text'}
        aria-label={title}
        aria-pressed={active}
        disabled={disabled}
        onClick={onClick}
      >
        {content}
      </Button>
    </Tooltip>
  )

  const openLink = () => {
    setHref((editor.getAttributes('link').href as string | undefined) ?? 'https://')
    setLinkOpen(true)
  }

  const applyLink = () => {
    const url = href.trim()
    if (url === '' || url === 'https://') chain().extendMarkRange('link').unsetLink().run()
    else if (!/^(https?:\/\/|mailto:)/i.test(url)) {
      message.error('A link must start with https://, http:// or mailto:.')
      return
    } else chain().extendMarkRange('link').setLink({ href: url }).run()
    setLinkOpen(false)
  }

  const insertImage = async (file: File) => {
    if (!IMAGE_TYPES.includes(file.type)) return message.error('The image must be a JPG or PNG file.')
    if (file.size > IMAGE_MAX_BYTES) return message.error('The image must be 2 MB or smaller.')
    setUploading(true)
    try {
      const src = await uploadImage!(file)
      // After the selection, never over it: text just made a link is still selected,
      // and setImage would replace it.
      chain()
        .setTextSelection(editor.state.selection.to)
        .setImage({ src, alt: file.name.replace(/\.[^.]+$/, '') })
        .run()
    } catch (e) {
      message.error(errorMessage(e))
    } finally {
      setUploading(false)
    }
  }

  return (
    <>
      <Flex wrap gap={2} align="center" style={{ padding: 4, borderBottom: '1px solid #f0f0f0' }}>
        {button('Heading 2', state.h2, () => chain().toggleHeading({ level: 2 }).run(), 'H2')}
        {button('Heading 3', state.h3, () => chain().toggleHeading({ level: 3 }).run(), 'H3')}
        {button('Heading 4', state.h4, () => chain().toggleHeading({ level: 4 }).run(), 'H4')}
        <Divider orientation="vertical" />
        {button('Bold', state.bold, () => chain().toggleBold().run(), <BoldOutlined />)}
        {button('Italic', state.italic, () => chain().toggleItalic().run(), <ItalicOutlined />)}
        {button('Underline', state.underline, () => chain().toggleUnderline().run(), <UnderlineOutlined />)}
        {button('Strikethrough', state.strike, () => chain().toggleStrike().run(), <StrikethroughOutlined />)}
        <Divider orientation="vertical" />
        {button('Bulleted list', state.bullet, () => chain().toggleBulletList().run(), <UnorderedListOutlined />)}
        {button('Numbered list', state.ordered, () => chain().toggleOrderedList().run(), <OrderedListOutlined />)}
        {button('Quote', state.quote, () => chain().toggleBlockquote().run(), '❝')}
        {button('Code block', state.code, () => chain().toggleCodeBlock().run(), <CodeOutlined />)}
        <Divider orientation="vertical" />
        {button('Link', state.link, openLink, <LinkOutlined />)}
        {uploadImage && (
          <Upload
            accept={IMAGE_TYPES.join(',')}
            showUploadList={false}
            beforeUpload={(file) => {
              void insertImage(file)
              return false
            }}
          >
            <Tooltip title="Image">
              <Button size="small" type="text" aria-label="Image" loading={uploading} icon={<PictureOutlined />} />
            </Tooltip>
          </Upload>
        )}
        <Divider orientation="vertical" />
        {button('Undo', false, () => chain().undo().run(), <UndoOutlined />, !state.canUndo)}
        {button('Redo', false, () => chain().redo().run(), <RedoOutlined />, !state.canRedo)}
      </Flex>

      <Modal title="Link" open={linkOpen} onOk={applyLink} onCancel={() => setLinkOpen(false)} okText="Apply" destroyOnHidden>
        <Input
          autoFocus
          value={href}
          onChange={(e) => setHref(e.target.value)}
          onPressEnter={applyLink}
          placeholder="https://"
        />
        <div style={{ marginTop: 8, color: '#8c8c8c' }}>Leave it empty to remove the link.</div>
      </Modal>
    </>
  )
}
