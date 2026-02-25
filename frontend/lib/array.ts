export function uniqueBy<T>(arr: T[], key: keyof T | ((item: T) => unknown)): T[] {
  const seen = new Set()
  return arr.filter((item) => {
    const k = typeof key === "function" ? key(item) : item[key]
    if (seen.has(k)) return false
    seen.add(k)
    return true
  })
}

export function groupBy<T>(arr: T[], key: keyof T | ((item: T) => string)): Record<string, T[]> {
  return arr.reduce(
    (groups, item) => {
      const k = String(typeof key === "function" ? key(item) : item[key])
      ;(groups[k] ??= []).push(item)
      return groups
    },
    {} as Record<string, T[]>
  )
}

export function sortBy<T>(arr: T[], key: keyof T | ((item: T) => unknown), order: "asc" | "desc" = "asc"): T[] {
  return [...arr].sort((a, b) => {
    const va = typeof key === "function" ? key(a) : a[key]
    const vb = typeof key === "function" ? key(b) : b[key]
    const cmp = va < vb ? -1 : va > vb ? 1 : 0
    return order === "asc" ? cmp : -cmp
  })
}
