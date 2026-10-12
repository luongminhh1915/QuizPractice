import { Button, Input, Select, Space, Typography } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import { Link, useLocation } from 'react-router'
import type { Gender, UserStatus, UserSummary } from '../../api/usersApi'
import { errorMessage } from '../../api/client'
import { DataTable, type DataTableColumn } from '../../components/DataTable'
import { PageHeader } from '../../components/PageHeader'
import { StatusTag } from '../../components/StatusTag'
import { useListParams } from '../../hooks/useListParams'
import { useUserChoices, useUserList } from '../../hooks/useUsers'

const toNumber = (value: string | undefined) => (value ? Number(value) : undefined)

const GENDERS: Gender[] = ['Male', 'Female', 'Other']
const STATUSES: UserStatus[] = ['Unverified', 'Active', 'Inactive']

/** Users List (SRS III 9.1; PDF II.9, F#37; UC-62 View Users List). Admin only. */
export function UsersListPage() {
  const location = useLocation()
  const list = useListParams()
  const gender = list.get('gender') as Gender | undefined
  const roleId = toNumber(list.get('roleId'))
  const status = list.get('status') as UserStatus | undefined

  // User Details returns to this list with the same criteria (SRS III 0.3).
  const back = { state: { back: location.pathname + location.search } }

  const query = useUserList({ ...list.criteria, gender, roleId, status })
  const choices = useUserChoices()

  const columns: DataTableColumn<UserSummary>[] = [
    { title: 'ID', key: 'id', sortable: true, width: 80 },
    { title: 'Full Name', key: 'fullName', sortable: true },
    { title: 'Gender', key: 'gender', sortable: true, width: 100 },
    { title: 'Email', key: 'email', sortable: true },
    { title: 'Mobile', key: 'mobile', sortable: true, width: 130 },
    { title: 'Role', key: 'role', sortable: true, width: 120 },
    { title: 'Status', key: 'status', sortable: true, width: 120, render: (u) => <StatusTag status={u.status} /> },
    {
      title: 'Action',
      key: 'action',
      width: 140,
      render: (u) => (
        <Space>
          <Link to={`/manage/users/${u.id}?view`} {...back}>
            <Button size="small">View</Button>
          </Link>
          <Link to={`/manage/users/${u.id}`} {...back}>
            <Button size="small">Edit</Button>
          </Link>
        </Space>
      ),
    },
  ]

  const error = query.error ?? choices.error

  return (
    <>
      <PageHeader
        title="Users List"
        action={
          <Link to="/manage/users/new" {...back}>
            <Button type="primary" icon={<PlusOutlined />}>
              Add New User
            </Button>
          </Link>
        }
      />

      <Space wrap style={{ marginBottom: 16 }}>
        {/* key resets the box when the criteria are cleared */}
        <Input.Search
          key={list.criteria.search ?? ''}
          defaultValue={list.criteria.search}
          placeholder="Search name, email or mobile"
          allowClear
          onSearch={(value) => list.set({ search: value.trim() })}
          style={{ width: 300 }}
        />
        <Select
          aria-label="Gender"
          value={gender ?? 'All'}
          onChange={(value) => list.set({ gender: value === 'All' ? undefined : value })}
          options={[{ value: 'All', label: 'All genders' }, ...GENDERS.map((g) => ({ value: g, label: g }))]}
          style={{ width: 140 }}
        />
        <Select
          aria-label="Role"
          value={roleId ?? 'All'}
          onChange={(value) => list.set({ roleId: value === 'All' ? undefined : value })}
          loading={choices.isPending}
          options={[{ value: 'All', label: 'All roles' }, ...(choices.data?.roles ?? []).map((r) => ({ value: r.id, label: r.name }))]}
          style={{ width: 150 }}
        />
        <Select
          aria-label="Status"
          value={status ?? 'All'}
          onChange={(value) => list.set({ status: value === 'All' ? undefined : value })}
          options={[{ value: 'All', label: 'All statuses' }, ...STATUSES.map((s) => ({ value: s, label: s }))]}
          style={{ width: 150 }}
        />
      </Space>

      {error && <Typography.Paragraph type="danger">{errorMessage(error)}</Typography.Paragraph>}

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
