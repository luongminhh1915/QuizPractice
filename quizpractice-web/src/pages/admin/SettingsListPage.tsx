import { App, Button, Input, Select, Space, Typography } from 'antd'
import { BankOutlined, PlusOutlined } from '@ant-design/icons'
import { Link, useLocation } from 'react-router'
import { SETTING_TYPE_LABELS, type SettingSummary, type SettingType } from '../../api/settingsApi'
import type { ActiveStatus } from '../../api/types'
import { errorMessage, problemOf } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useListParams } from '../../hooks/useListParams'
import { useChangeSettingStatus, useSettingList } from '../../hooks/useSettings'

const TYPES = Object.entries(SETTING_TYPE_LABELS) as [SettingType, string][]

/**
 * Settings List (SRS III 9.3; PDF II.9, F#39; UC-64 View Settings List, UC-66 Activate or
 * Deactivate Setting). Admin only.
 */
export function SettingsListPage() {
  const { message, modal } = App.useApp()
  const location = useLocation()
  const list = useListParams()
  const type = list.get('type') as SettingType | undefined
  const status = list.get('status') as ActiveStatus | undefined

  const query = useSettingList({ ...list.criteria, type, status })

  const changeStatus = useChangeSettingStatus()

  const toggle = (setting: SettingSummary, confirmInUse = false) =>
    changeStatus.mutate(
      { setting, confirmInUse },
      {
        onSuccess: (saved) => message.success(`"${saved.value}" is now ${saved.status}.`),
        onError: (e) => {
          const code = problemOf(e)?.code
          // E1: records still use the value; they keep it, the screens stop offering it.
          if (code === 'Setting.InUse') {
            modal.confirm({
              title: 'Switch off a value in use?',
              content: errorMessage(e),
              okText: 'Switch off',
              cancelText: 'Cancel',
              onOk: () => toggle(setting, true),
            })
            return
          }
          // E2: the list has reloaded with the current status.
          message.error(errorMessage(e), 8)
        },
      },
    )

  // Setting Details returns to this list with the same criteria (SRS III 0.3).
  const back = { state: { back: location.pathname + location.search } }

  const columns: DataTableColumn<SettingSummary>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 80 },
    { title: 'Type', key: 'type', sortable: true, width: 200, render: (s) => SETTING_TYPE_LABELS[s.type] },
    { title: 'Value', key: 'value', sortable: true },
    { title: 'Order', key: 'order', sortable: true, width: 100, render: (s) => s.displayOrder },
    { title: 'Status', key: 'status', sortable: true, width: 120, render: (s) => <StatusTag status={s.status} /> },
    {
      title: 'Action',
      key: 'action',
      width: 250,
      render: (s) => (
        <Space>
          <Link to={`/manage/settings/${s.id}?view`} {...back}>
            <Button size="small">View</Button>
          </Link>
          <Link to={`/manage/settings/${s.id}`} {...back}>
            <Button size="small">Edit</Button>
          </Link>
          {/* UC-66: the label names the action available; a value the system needs stays Active. */}
          {(s.status === 'Inactive' || s.canDeactivate) && (
            <Button
              size="small"
              danger={s.status === 'Active'}
              loading={changeStatus.isPending && changeStatus.variables?.setting.id === s.id}
              onClick={() => toggle(s)}
            >
              {s.status === 'Active' ? 'Deactivate' : 'Activate'}
            </Button>
          )}
        </Space>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Settings List"
        action={
          <Space>
            {/* UC-70: the receiving account is not a setting type, so it has its own screen (9.5). */}
            <Link to="/manage/settings/bank-account">
              <Button icon={<BankOutlined />}>Bank Account</Button>
            </Link>
            <Link to="/manage/settings/new" {...back}>
              <Button type="primary" icon={<PlusOutlined />}>
                Add New Setting
              </Button>
            </Link>
          </Space>
        }
      />

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
