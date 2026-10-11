import { Space, Typography } from 'antd'
import type { Price } from '../../api/publicApi'

const vnd = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 })

/** Price of SRS III 2.4, 2.5: list price struck through, sale price and duration of the cheapest package. */
export function PriceTag({ price }: { price: Price | null }) {
  if (!price) return <Typography.Text type="secondary">No package available</Typography.Text>
  return (
    <Space size={8} wrap>
      {price.listPrice > price.salePrice && (
        <Typography.Text delete type="secondary">
          {vnd.format(price.listPrice)}
        </Typography.Text>
      )}
      <Typography.Text strong style={{ color: '#d4380d' }}>
        {vnd.format(price.salePrice)}
      </Typography.Text>
      <Typography.Text type="secondary">
        / {price.accessMonths} {price.accessMonths === 1 ? 'month' : 'months'}
      </Typography.Text>
    </Space>
  )
}
