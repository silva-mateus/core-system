"use client"

import { AlertTriangle } from "lucide-react"
import { Button } from "@core/components/ui/button"
import { cn } from "@core/lib/utils"

interface ErrorStateProps {
  message: string
  onRetry?: () => void
  retryLabel?: string
  className?: string
}

export function ErrorState({
  message,
  onRetry,
  retryLabel = "Tentar novamente",
  className,
}: ErrorStateProps) {
  return (
    <div className={cn("text-center py-8", className)}>
      <div className="flex flex-col items-center gap-4">
        <div className="p-3 bg-destructive/10 rounded-full">
          <AlertTriangle className="h-6 w-6 text-destructive" />
        </div>
        <p className="text-destructive text-sm">{message}</p>
        {onRetry && (
          <Button onClick={onRetry} variant="outline" size="sm">
            {retryLabel}
          </Button>
        )}
      </div>
    </div>
  )
}
