import { createBrowserRouter } from 'react-router'
import { ManagementLayout } from '../layouts/ManagementLayout'
import { PublicLayout } from '../layouts/PublicLayout'
import { NotFoundPage } from '../pages/NotFoundPage'
import { PostDetailsPage } from '../pages/marketing/PostDetailsPage'
import { PostsListPage } from '../pages/marketing/PostsListPage'
import { SliderDetailsPage } from '../pages/marketing/SliderDetailsPage'
import { SlidersListPage } from '../pages/marketing/SlidersListPage'
import { HomePage } from '../pages/public/HomePage'
import { QuestionDetailsPage } from '../pages/testcontent/QuestionDetailsPage'
import { QuestionsListPage } from '../pages/testcontent/QuestionsListPage'

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
      { path: '/', element: <HomePage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
  {
    path: '/manage',
    element: <ManagementLayout />,
    children: [
      { path: 'posts', element: <PostsListPage /> },
      { path: 'posts/new', element: <PostDetailsPage /> },
      { path: 'posts/:id', element: <PostDetailsPage /> },
      { path: 'sliders', element: <SlidersListPage /> },
      { path: 'sliders/new', element: <SliderDetailsPage /> },
      { path: 'sliders/:id', element: <SliderDetailsPage /> },
      { path: 'questions', element: <QuestionsListPage /> },
      { path: 'questions/new', element: <QuestionDetailsPage /> },
      { path: 'questions/:id', element: <QuestionDetailsPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
