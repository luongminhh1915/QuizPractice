import { Button, Result, Spin, Tabs } from 'antd'
import { useLocation, useNavigate, useParams } from 'react-router'
import { errorMessage, problemOf } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { useSubject } from '../../hooks/useSubjects'
import { SubjectOverviewTab } from './SubjectOverviewTab'

/**
 * Subject Management Details (SRS III 6.3; PDF II.6, F#25): the Overview tab of a subject
 * for the Admin and its assigned Expert (UC-43 to UC-45).
 */
export function SubjectManagementDetailsPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const id = Number(useParams().id)
  const subject = useSubject(id)

  const backTo = (location.state as { back?: string } | null)?.back ?? '/manage/subjects'

  if (subject.isPending) return <Spin />
  if (subject.isError) {
    // UC-44 E1: an Expert on a subject of another Expert is refused and goes back to the list.
    const status = problemOf(subject.error)?.status
    return (
      <Result
        status={status === 404 ? '404' : status === 403 ? '403' : 'error'}
        title={status === 404 ? 'This subject does not exist.' : errorMessage(subject.error)}
        extra={<Button onClick={() => navigate(backTo)}>Back to Subject Management List</Button>}
      />
    )
  }

  return (
    <>
      <PageHeader title={subject.data.name} />
      <Tabs
        items={[{ key: 'overview', label: 'Overview', children: <SubjectOverviewTab subject={subject.data} backTo={backTo} /> }]}
      />
    </>
  )
}
