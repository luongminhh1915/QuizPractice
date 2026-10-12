import { Button, Result, Spin, Tabs } from 'antd'
import { Link } from 'react-router'
import { useLocation, useNavigate, useParams, useSearchParams } from 'react-router'
import { errorMessage, problemOf } from '../../api/client'
import { PageHeader } from '../../components/PageHeader'
import { useSubject } from '../../hooks/useSubjects'
import { SubjectDimensionTab } from './SubjectDimensionTab'
import { SubjectPackageTab } from './SubjectPackageTab'
import { SubjectOverviewTab } from './SubjectOverviewTab'

/**
 * Subject Management Details (SRS III 6.3; PDF II.6, F#25 to F#27): the Overview,
 * Dimension and Price Package tabs of a subject for the Admin and its assigned Expert (UC-43 to UC-48).
 */
export function SubjectManagementDetailsPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const id = Number(useParams().id)
  const subject = useSubject(id)
  // The open tab stays in the address, so a reload or Back keeps it.
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') ?? 'overview'

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
      <PageHeader
        title={subject.data.name}
        action={
          <Link to={`/manage/subjects/${id}/lessons`} state={location.state}>
            <Button>Lessons</Button>
          </Link>
        }
      />
      <Tabs
        activeKey={tab}
        onChange={(key) => setParams(key === 'overview' ? {} : { tab: key }, { replace: true, state: location.state })}
        items={[
          { key: 'overview', label: 'Overview', children: <SubjectOverviewTab subject={subject.data} backTo={backTo} /> },
          { key: 'dimension', label: 'Dimension', children: <SubjectDimensionTab subjectId={id} /> },
          { key: 'package', label: 'Price Package', children: <SubjectPackageTab subjectId={id} /> },
        ]}
      />
    </>
  )
}
