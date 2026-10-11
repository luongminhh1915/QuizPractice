import { Button, Flex, Tag, Typography } from 'antd'

/**
 * Criteria of SRS III 2.2 and 2.4: the search text and the category applied to the
 * list, each with a cross to remove it, and Clear to restore the default list.
 */
export function CriteriaBar({
  total,
  noun,
  criteria,
  onClear,
}: {
  total: number | undefined
  noun: string
  criteria: { label: string; onRemove: () => void }[]
  onClear: () => void
}) {
  return (
    <Flex justify="space-between" align="center" wrap gap={8} style={{ marginBottom: 16 }}>
      <Flex gap={8} align="center" wrap>
        {criteria.map((c) => (
          <Tag key={c.label} closable onClose={c.onRemove}>
            {c.label}
          </Tag>
        ))}
        {!!criteria.length && (
          <Button size="small" type="link" onClick={onClear}>
            Clear
          </Button>
        )}
      </Flex>
      {total !== undefined && (
        <Typography.Text type="secondary">
          {total} {total === 1 ? noun : `${noun}s`}
        </Typography.Text>
      )}
    </Flex>
  )
}
