import { createBrowserRouter } from 'react-router'
import { AppShell } from '../layouts/AppShell'
import { ManagementLayout } from '../layouts/ManagementLayout'
import { PublicLayout } from '../layouts/PublicLayout'
import { NotFoundPage } from '../pages/NotFoundPage'
import { ResetPasswordPage } from '../pages/common/ResetPasswordPage'
import { VerifyEmailPage } from '../pages/common/VerifyEmailPage'
import { MyRegistrationsPage } from '../pages/customer/MyRegistrationsPage'
import { DashboardPage } from '../pages/marketing/DashboardPage'
import { PostDetailsPage } from '../pages/marketing/PostDetailsPage'
import { PostsListPage } from '../pages/marketing/PostsListPage'
import { SliderDetailsPage } from '../pages/marketing/SliderDetailsPage'
import { SlidersListPage } from '../pages/marketing/SlidersListPage'
import { BlogDetailsPage } from '../pages/public/BlogDetailsPage'
import { BlogsListPage } from '../pages/public/BlogsListPage'
import { HomePage } from '../pages/public/HomePage'
import { SubjectDetailsPage } from '../pages/public/SubjectDetailsPage'
import { SubjectsListPage } from '../pages/public/SubjectsListPage'
import { QuestionDetailsPage } from '../pages/testcontent/QuestionDetailsPage'
import { QuestionsListPage } from '../pages/testcontent/QuestionsListPage'
import { QuizDetailsPage } from '../pages/testcontent/QuizDetailsPage'
import { QuizzesListPage } from '../pages/testcontent/QuizzesListPage'

/**
 * Every screen of SRS III gets one route here. Public and customer pages sit under
 * PublicLayout (III 0.2); Marketing, Sale, Expert and Admin screens under /manage with
 * ManagementLayout (III 0.1), which refuses a screen the role's menu does not have
 * (BR-03). A new screen also needs ready: true in constants/menu.tsx.
 */
export const router = createBrowserRouter([
  {
    element: <AppShell />,
    children: [
      {
        element: <PublicLayout />,
        children: [
          { path: '/', element: <HomePage /> },
          { path: '/subjects', element: <SubjectsListPage /> },
          { path: '/subjects/:id', element: <SubjectDetailsPage /> },
          { path: '/blogs', element: <BlogsListPage /> },
          { path: '/blogs/:id', element: <BlogDetailsPage /> },
          { path: '/my-registrations', element: <MyRegistrationsPage /> },
          { path: '/verify-email', element: <VerifyEmailPage /> },
          { path: '/reset-password', element: <ResetPasswordPage /> },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
      {
        path: '/manage',
        element: <ManagementLayout />,
        children: [
          { path: 'dashboard', element: <DashboardPage /> },
          { path: 'posts', element: <PostsListPage /> },
          { path: 'posts/new', element: <PostDetailsPage /> },
          { path: 'posts/:id', element: <PostDetailsPage /> },
          { path: 'sliders', element: <SlidersListPage /> },
          { path: 'sliders/new', element: <SliderDetailsPage /> },
          { path: 'sliders/:id', element: <SliderDetailsPage /> },
          { path: 'questions', element: <QuestionsListPage /> },
          { path: 'questions/new', element: <QuestionDetailsPage /> },
          { path: 'questions/:id', element: <QuestionDetailsPage /> },
          { path: 'quizzes', element: <QuizzesListPage /> },
          { path: 'quizzes/new', element: <QuizDetailsPage /> },
          { path: 'quizzes/:id', element: <QuizDetailsPage /> },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
])
