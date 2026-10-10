/** Select options grouped under a heading (a topic, a dimension type), in the order the API gave. */
export function groupedOptions<T extends { id: number; name: string }>(items: T[], groupOf: (item: T) => string) {
  const groups = new Map<string, { value: number; label: string }[]>()
  for (const item of items) {
    const group = groupOf(item)
    if (!groups.has(group)) groups.set(group, [])
    groups.get(group)!.push({ value: item.id, label: item.name })
  }
  return [...groups].map(([label, options]) => ({ label, title: label, options }))
}
