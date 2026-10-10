import { Input, Select, Space, Typography } from 'antd'
import type { Slider } from '../../api/slidersApi'
import type { VisibilityStatus } from '../../api/types'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useListParams } from '../../hooks/useListParams'
import { useSliderList } from '../../hooks/useSliders'

/**
 * Sliders List (SRS III 5.4; F#21; UC-38 View Sliders List).
 * The sample list screen: copy its shape for every other list.
 */
export function SlidersListPage() {
  const list = useListParams()
  const status = list.get('status') as VisibilityStatus | undefined
  const query = useSliderList({ ...list.criteria, status })

  const columns: DataTableColumn<Slider>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 80 },
    {
      title: 'Image',
      key: 'image',
      width: 110,
      render: (s) => (
        <img src={s.image} alt="" style={{ width: 88, height: 40, objectFit: 'cover', borderRadius: 4 }} />
      ),
    },
    { title: 'Title', key: 'title', sortable: true },
    {
      title: 'Backlink',
      key: 'backlink',
      sortable: true,
      render: (s) =>
        s.backlink ? (
          <Typography.Link href={s.backlink} target="_blank" rel="noopener noreferrer" ellipsis style={{ maxWidth: 280 }}>
            {s.backlink}
          </Typography.Link>
        ) : (
          <Typography.Text type="secondary">—</Typography.Text>
        ),
    },
    { title: 'Status', key: 'status', sortable: true, width: 110, render: (s) => <StatusTag status={s.status} /> },
  ]

  return (
    <>
      <PageHeader title="Sliders List" />

      <Space wrap style={{ marginBottom: 16 }}>
        {/* key resets the box when the criteria are cleared */}
        <Input.Search
          key={list.criteria.search ?? ''}
          defaultValue={list.criteria.search}
          placeholder="Search title or backlink"
          allowClear
          onSearch={(value) => list.set({ search: value.trim() })}
          style={{ width: 300 }}
        />
        <Select
          value={status ?? 'All'}
          onChange={(value) => list.set({ status: value === 'All' ? undefined : value })}
          options={[{ value: 'All' }, { value: 'Shown' }, { value: 'Hidden' }]}
          style={{ width: 140 }}
        />
      </Space>

      {query.isError && (
        <Typography.Paragraph type="danger">{errorMessage(query.error)}</Typography.Paragraph>
      )}

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
