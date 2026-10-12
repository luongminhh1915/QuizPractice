import { useState } from 'react'
import { Button, Empty, Flex, Modal, Segmented, Typography } from 'antd'
import { FlagFilled } from '@ant-design/icons'

/** One question number in the pop-up. */
export interface NumberItem {
  order: number
  answered: boolean
  marked: boolean
  /** Quiz Review only: answered wrongly or not at all (BR-21). */
  incorrect?: boolean
}

export interface NumberFilter {
  key: string
  label: string
  match: (item: NumberItem) => boolean
}

/**
 * The question numbers of an attempt, filtered (SRS III 4.1 Review Progress, UC-27;
 * 4.2 Review Result, UC-32). Answered numbers are filled, marked ones carry a flag, the
 * current one is outlined. Choosing a number closes the pop-up and opens that question.
 */
export function QuestionNumbersModal({
  open,
  title,
  items,
  filters,
  current,
  summary,
  footer,
  onPick,
  onClose,
}: {
  open: boolean
  title: string
  items: NumberItem[]
  filters: NumberFilter[]
  current: number
  summary?: React.ReactNode
  footer?: React.ReactNode
  onPick: (order: number) => void
  onClose: () => void
}) {
  const [filterKey, setFilterKey] = useState(filters[filters.length - 1].key)
  const filter = filters.find((f) => f.key === filterKey) ?? filters[0]
  const shown = items.filter(filter.match)

  return (
    <Modal open={open} title={title} onCancel={onClose} footer={footer ?? null} width={560} destroyOnHidden>
      <Flex vertical gap={16}>
        {summary}
        <Segmented
          block
          value={filterKey}
          onChange={(v) => setFilterKey(v as string)}
          options={filters.map((f) => ({ value: f.key, label: `${f.label} (${items.filter(f.match).length})` }))}
        />
        {/* E1: an empty filter says so; the other filters stay reachable. */}
        {shown.length === 0 ? (
          <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={`No question under ${filter.label}.`} />
        ) : (
          <Flex wrap gap={8}>
            {shown.map((item) => (
              <Button
                key={item.order}
                type={item.answered ? 'primary' : 'default'}
                style={{
                  width: 52,
                  position: 'relative',
                  outline: item.order === current ? '2px solid #faad14' : undefined,
                  outlineOffset: 2,
                }}
                aria-label={`Question ${item.order}`}
                onClick={() => onPick(item.order)}
              >
                {item.order}
                {item.marked && <FlagFilled style={{ position: 'absolute', top: -6, right: -6, color: '#fa8c16', fontSize: 12 }} />}
              </Button>
            ))}
          </Flex>
        )}
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          Filled: answered · <FlagFilled style={{ color: '#fa8c16' }} /> marked · outlined: the question on screen
        </Typography.Text>
      </Flex>
    </Modal>
  )
}
