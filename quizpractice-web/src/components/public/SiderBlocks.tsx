import { Card, Flex, Image, Input, Menu, Typography } from 'antd'
import { MailOutlined, PhoneOutlined, EnvironmentOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import { Link } from 'react-router'
import type { PostCard, SubjectCard } from '../../api/publicApi'

/**
 * The blocks of the public sider (SRS III 0.2). Each page picks the ones its
 * spreadsheet row names; a block with nothing to show is left out (UC-09 A1).
 */

/** Search box: searches the posts or the subjects and opens the matching list (F#8, F#10). */
export function SearchBlock({ placeholder, value, onSearch }: { placeholder: string; value?: string; onSearch: (text: string) => void }) {
  return (
    <Card size="small" title="Search">
      {/* key: a new value from the address (Clear, Back) refills the box. */}
      <Input.Search key={value ?? ''} defaultValue={value} placeholder={placeholder} allowClear onSearch={(t) => onSearch(t.trim())} />
    </Card>
  )
}

/** The post or subject categories of the system settings (BR-16); selecting one filters the list. */
export function CategoriesBlock({
  categories,
  selected,
  onSelect,
}: {
  categories: { id: number; name: string }[]
  selected?: number
  onSelect: (id: number | undefined) => void
}) {
  if (!categories.length) return null
  return (
    <Card size="small" title="Categories" styles={{ body: { padding: 4 } }}>
      <Menu
        mode="inline"
        selectedKeys={selected ? [String(selected)] : []}
        style={{ borderInlineEnd: 0 }}
        items={categories.map((c) => ({ key: String(c.id), label: c.name }))}
        onClick={({ key }) => onSelect(Number(key) === selected ? undefined : Number(key))}
      />
    </Card>
  )
}

/** The most recently updated shown posts, each a link to Blog Details. */
export function LatestPostsBlock({ posts }: { posts: PostCard[] | undefined }) {
  if (!posts?.length) return null
  return (
    <Card size="small" title="Latest posts">
      <Flex vertical gap={12}>
        {posts.map((p) => (
          <Link key={p.id} to={`/blogs/${p.id}`}>
            <Flex gap={10} align="center">
              <Image src={p.thumbnail} width={64} height={36} preview={false} style={{ objectFit: 'cover', borderRadius: 4 }} />
              <Flex vertical style={{ minWidth: 0 }}>
                <Typography.Text ellipsis style={{ maxWidth: 170 }}>
                  {p.title}
                </Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {dayjs(p.updatedAt).format('DD/MM/YYYY')}
                </Typography.Text>
              </Flex>
            </Flex>
          </Link>
        ))}
      </Flex>
    </Card>
  )
}

/** Published subjects whose featured flag is on, each a link to Subject Details. */
export function FeaturedSubjectsBlock({ subjects }: { subjects: SubjectCard[] | undefined }) {
  if (!subjects?.length) return null
  return (
    <Card size="small" title="Featured subjects">
      <Flex vertical gap={12}>
        {subjects.map((s) => (
          <Link key={s.id} to={`/subjects/${s.id}`}>
            <Flex gap={10} align="center">
              <Image src={s.thumbnail} width={64} height={36} preview={false} style={{ objectFit: 'cover', borderRadius: 4 }} />
              <Typography.Text ellipsis style={{ maxWidth: 170 }}>
                {s.name}
              </Typography.Text>
            </Flex>
          </Link>
        ))}
      </Flex>
    </Card>
  )
}

/**
 * Static contacts and links of the organization, the same on every page (SRS III 0.2).
 * Placeholder details: the team puts the real ones here.
 */
export function ContactsBlock() {
  return (
    <Card size="small" title="Contacts and links">
      <Flex vertical gap={6}>
        <Typography.Text>
          <MailOutlined /> support@qps.vn
        </Typography.Text>
        <Typography.Text>
          <PhoneOutlined /> 1900 0000
        </Typography.Text>
        <Typography.Text>
          <EnvironmentOutlined /> Hoa Lac Hi-Tech Park, Hanoi
        </Typography.Text>
        <Link to="/subjects">All subjects</Link>
        <Link to="/blogs">All posts</Link>
      </Flex>
    </Card>
  )
}
