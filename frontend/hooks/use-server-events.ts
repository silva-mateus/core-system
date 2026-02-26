"use client"

import { useEffect, useRef, useCallback } from "react"

type EventHandlers = Record<string, (data: any) => void>

interface UseServerEventsOptions {
  enabled?: boolean
  withCredentials?: boolean
}

export function useServerEvents(
  url: string,
  handlers: EventHandlers,
  options: UseServerEventsOptions = {}
) {
  const { enabled = true, withCredentials = true } = options
  const handlersRef = useRef(handlers)
  handlersRef.current = handlers

  const sourceRef = useRef<EventSource | null>(null)

  const disconnect = useCallback(() => {
    if (sourceRef.current) {
      sourceRef.current.close()
      sourceRef.current = null
    }
  }, [])

  useEffect(() => {
    if (!enabled) {
      disconnect()
      return
    }

    const source = new EventSource(url, { withCredentials })
    sourceRef.current = source

    const eventNames = Object.keys(handlersRef.current)
    for (const eventName of eventNames) {
      source.addEventListener(eventName, (e: MessageEvent) => {
        try {
          const data = JSON.parse(e.data)
          handlersRef.current[eventName]?.(data)
        } catch {
          handlersRef.current[eventName]?.(e.data)
        }
      })
    }

    return () => {
      source.close()
      sourceRef.current = null
    }
  }, [url, enabled, withCredentials, disconnect])

  return { disconnect }
}
