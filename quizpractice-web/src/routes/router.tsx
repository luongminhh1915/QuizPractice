import { createBrowserRouter } from 'react-router'
import { ManagementLayout } from '../layouts/ManagementLayout'
import { PublicLayout } from '../layouts/PublicLayout'
import { NotFoundPage } from '../pages/NotFoundPage'
import { SlidersListPage } from '../pages/marketing/SlidersListPage'

/**
 * Every screen of SRS III gets one route here. Public and customer pages sit under
 * PublicLayout (III 0.2); Marketing, Sale, Expert and Admin screens under /manage with
 * ManagementLayout (III 0.1).
 *
 * TODO(auth): a role guard on /manage, from the same permission table the API uses
 * (BR-03, D23). It only hides screens; the API checks every request again.
 */
export const router = createBrowserRouter([
  {
    element: <PublicLayout />,
    children: [
      { path: '*', element: <NotFoundPage /> },
    ],
  },
  {
    path: '/manage',
    element: <ManagementLayout />,
    children: [
      { path: 'sliders', element: <SlidersListPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
