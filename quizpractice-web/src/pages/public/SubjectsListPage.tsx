import { Button, Col, Empty, Pagination, Result, Row, Spin, Typography } from 'antd'
import { errorMessage } from '../../api/client'
import { SubjectCardView } from '../../components/public/Cards'
import { CriteriaBar } from '../../components/public/CriteriaBar'
import { CategoriesBlock, ContactsBlock, FeaturedSubjectsBlock, SearchBlock } from '../../components/public/SiderBlocks'
import { useListParams } from '../../hooks/useListParams'
import { useSubjectSider, useSubjects } from '../../hooks/usePublic'
import { PublicSider } from '../../layouts/PublicLayout'

/** Three rows of three cards. */
const SUBJECT_PAGE_SIZE = 9

/**
 * Subjects List (SRS III 2.4; F#10; UC-12): published subjects, most recently updated
 * first, each with the prices of its cheapest active package. The criteria sit in the
 * address; changing one returns to page 1 (A3), moving between pages keeps them (A4).
 */
export function SubjectsListPage() {
  const { criteria, get, set, clear } = useListParams()
  const categoryId = get('categoryId') ? Number(get('categoryId')) : undefined
  const subjects = useSubjects({ page: criteria.page, pageSize: SUBJECT_PAGE_SIZE, search: criteria.search, categoryId })
  const sider = useSubjectSider()
  const category = sider.data?.categories.find((c) => c.id === categoryId)

  const applied = [
    ...(criteria.search ? [{ label: `Search: ${criteria.search}`, onRemove: () => set({ search: undefined }) }] : []),
    ...(categoryId ? [{ label: `Category: ${category?.name ?? categoryId}`, onRemove: () => set({ categoryId: undefined }) }] : []),
  ]

  return (
    <PublicSider
      blocks={
        <>
          <SearchBlock placeholder="Search subjects by name" value={criteria.search} onSearch={(search) => set({ search })} />
          <CategoriesBlock categories={sider.data?.categories ?? []} selected={categoryId} onSelect={(id) => set({ categoryId: id })} />
          <FeaturedSubjectsBlock subjects={sider.data?.featuredSubjects} />
          <ContactsBlock />
        </>
      }
    >
      <Typography.Title level={3}>Subjects</Typography.Title>
      <CriteriaBar total={subjects.data?.totalCount} noun="subject" criteria={applied} onClear={clear} />

      {subjects.isPending && <Spin style={{ display: 'block', padding: 48 }} />}
      {subjects.isError && <Result status="warning" title={errorMessage(subjects.error)} />}

      {/* E1: nothing matches; the criteria stay and can be cleared. */}
      {subjects.data && subjects.data.items.length === 0 && (
        <Empty description={applied.length ? 'No subject matches these criteria.' : 'No subject has been published yet.'}>
          {!!applied.length && <Button onClick={clear}>Clear the criteria</Button>}
        </Empty>
      )}

      {!!subjects.data?.items.length && (
        <>
          <Row gutter={[16, 16]}>
            {subjects.data.items.map((s) => (
              <Col key={s.id} xs={24} sm={12} lg={8}>
                <SubjectCardView subject={s} withPrice />
              </Col>
            ))}
          </Row>
          <Pagination
            style={{ marginTop: 24, justifyContent: 'center' }}
            current={subjects.data.pageNumber}
            pageSize={subjects.data.pageSize}
            total={subjects.data.totalCount}
            showSizeChanger={false}
            hideOnSinglePage
            onChange={(page) => set({ page })}
          />
        </>
      )}
    </PublicSider>
  )
}
