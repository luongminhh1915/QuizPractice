import { Input, Select, Space, Typography } from 'antd'
import { SETTING_TYPE_LABELS, type SettingSummary, type SettingType } from '../../api/settingsApi'
import type { ActiveStatus } from '../../api/types'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useListParams } from '../../hooks/useListParams'
import { useSettingList } from '../../hooks/useSettings'

const TYPES = Object.entries(SETTING_TYPE_LABELS) as [SettingType, string][]

/** Settings List (SRS III 9.3; PDF II.9, F#39; UC-64 View Settings List). Admin only. */
export function SettingsListPage() {
  const list = useListParams()
  const type = list.get('type') as SettingType | undefined
  const status = list.get('status') as ActiveStatus | undefined

  const query = useSettingList({ ...list.criteria, type, status })

  const columns: DataTableColumn<SettingSummary>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 80 },
    { title: 'Type', key: 'type', sortable: true, width: 200, render: (s) => SETTING_TYPE_LABELS[s.type] },
    { title: 'Value', key: 'value', sortable: true },
    { title: 'Order', key: 'order', sortable: true, width: 100, render: (s) => s.displayOrder },
    { title: 'Status', key: 'status', sortable: true, width: 120, render: (s) => <StatusTag status={s.status} /> },
  ]

  return (
    <>
      <PageHeader title="Settings List" />

      <Space wrap style={{ marginBottom: 16 }}>
        {/* key resets the box when the criteria are cleared */}
        <Input.Search
          key={list.criteria.search ?? ''}
          defaultValue={list.criteria.search}
          placeholder="Search value"
          allowClear
          onSearch={(value) => list.set({ search: value.trim() })}
          style={{ width: 260 }}
        />
        <Select
          aria-label="Type"
          value={type ?? 'All'}
          onChange={(value) => list.set({ type: value === 'All' ? undefined : value })}
          options={[{ value: 'All', label: 'All types' }, ...TYPES.map(([value, label]) => ({ value, label }))]}
          popupMatchSelectWidth={false}
          style={{ width: 200 }}
        />
        <Select
          aria-label="Status"
          value={status ?? 'All'}
          onChange={(value) => list.set({ status: value === 'All' ? undefined : value })}
          options={[
            { value: 'All', label: 'All statuses' },
            { value: 'Active', label: 'Active' },
            { value: 'Inactive', label: 'Inactive' },
          ]}
          style={{ width: 150 }}
        />
      </Space>

      {query.error && <Typography.Paragraph type="danger">{errorMessage(query.error)}</Typography.Paragraph>}

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
