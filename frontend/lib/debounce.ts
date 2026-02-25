export function debounce<T extends (...args: unknown[]) => unknown>(fn: T, delay: number): T & { cancel: () => void } {
  let timeoutId: ReturnType<typeof setTimeout> | null = null

  const debounced = (...args: Parameters<T>) => {
    if (timeoutId) clearTimeout(timeoutId)
    timeoutId = setTimeout(() => fn(...args), delay)
  }

  debounced.cancel = () => {
    if (timeoutId) clearTimeout(timeoutId)
  }

  return debounced as T & { cancel: () => void }
}

export function throttle<T extends (...args: unknown[]) => unknown>(fn: T, limit: number): T {
  let inThrottle = false

  const throttled = (...args: Parameters<T>) => {
    if (!inThrottle) {
      fn(...args)
      inThrottle = true
      setTimeout(() => (inThrottle = false), limit)
    }
  }

  return throttled as unknown as T
}
