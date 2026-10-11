import { Button, Dropdown, type MenuProps } from 'antd'
import { DownOutlined } from '@ant-design/icons'
import { Link } from 'react-router'
import { MENU_SCREENS } from '../../constants/menu'
import { useCurrentUser } from '../../hooks/useAccount'

/**
 * My Learning of SRS III 0.2: the customer screens of the role menu (D23). Shown only
 * when the menu has any, i.e. to a signed-in customer. A screen not built yet is greyed.
 */
export function MyLearningMenu() {
  const me = useCurrentUser()
  const items: MenuProps['items'] = (me.data?.menu ?? [])
    .map((item) => ({ item, screen: MENU_SCREENS[item] }))
    .filter(({ screen }) => screen?.area === 'learning')
    .map(({ item, screen }) => ({
      key: item,
      icon: screen.icon,
      disabled: !screen.ready,
      label: screen.ready ? <Link to={screen.path}>{item}</Link> : `${item} (coming soon)`,
    }))

  if (!items.length) return null
  return (
    <Dropdown menu={{ items }}>
      <Button type="text">
        My Learning <DownOutlined />
      </Button>
    </Dropdown>
  )
}
