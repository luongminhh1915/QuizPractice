import type { ReactNode } from 'react'
import {
  AppstoreOutlined,
  BookOutlined,
  DashboardOutlined,
  FileDoneOutlined,
  FileTextOutlined,
  FormOutlined,
  PictureOutlined,
  QuestionCircleOutlined,
  SettingOutlined,
  ShoppingCartOutlined,
  TeamOutlined,
} from '@ant-design/icons'

/**
 * Screen of each System Menu item (D23). The API sends the items the signed-in role
 * may open (CurrentUser.menu); this map says where each one goes. Management items
 * fill the sider of III 0.1, customer items the My Learning menu of III 0.2.
 *
 * ready: the screen exists in this build. An item that is not ready yet stays in the
 * menu, greyed out, so everyone sees what the role will get. Set it to true together
 * with the route in router.tsx.
 */
export interface MenuScreen {
  path: string
  area: 'manage' | 'learning'
  icon: ReactNode
  ready: boolean
}

export const MENU_SCREENS: Record<string, MenuScreen> = {
  Dashboard: { path: '/manage/dashboard', area: 'manage', icon: <DashboardOutlined />, ready: true },
  Posts: { path: '/manage/posts', area: 'manage', icon: <FileTextOutlined />, ready: true },
  Sliders: { path: '/manage/sliders', area: 'manage', icon: <PictureOutlined />, ready: true },
  Registrations: { path: '/manage/registrations', area: 'manage', icon: <ShoppingCartOutlined />, ready: true },
  Invoices: { path: '/manage/invoices', area: 'manage', icon: <FileDoneOutlined />, ready: true },
  Subjects: { path: '/manage/subjects', area: 'manage', icon: <BookOutlined />, ready: true },
  Questions: { path: '/manage/questions', area: 'manage', icon: <QuestionCircleOutlined />, ready: true },
  Quizzes: { path: '/manage/quizzes', area: 'manage', icon: <FormOutlined />, ready: true },
  Users: { path: '/manage/users', area: 'manage', icon: <TeamOutlined />, ready: false },
  Settings: { path: '/manage/settings', area: 'manage', icon: <SettingOutlined />, ready: false },
  'My Registrations': { path: '/my-registrations', area: 'learning', icon: <ShoppingCartOutlined />, ready: true },
  Practices: { path: '/practices', area: 'learning', icon: <AppstoreOutlined />, ready: true },
  'Simulation Exams': { path: '/simulation-exams', area: 'learning', icon: <FormOutlined />, ready: true },
  'My Invoices': { path: '/my-invoices', area: 'learning', icon: <FileDoneOutlined />, ready: true },
}

/**
 * Where a role lands after signing in (UC-01 post-condition): the first ready
 * management screen of its menu. Undefined for a customer, who stays on the page.
 */
export function firstScreen(menu: string[]): string | undefined {
  return menu.map((item) => MENU_SCREENS[item]).find((s) => s?.area === 'manage' && s.ready)?.path
}
