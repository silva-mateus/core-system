"use client"

import { Loader2 } from "lucide-react"
import { cn } from "@core/lib/utils"

interface LoadingSpinnerProps {
  message?: string
  className?: string
  size?: "sm" | "md" | "lg"
}

const sizeClasses = {
  sm: "h-4 w-4",
  md: "h-8 w-8",
  lg: "h-12 w-12",
}

export function LoadingSpinner({
  message,
  className,
  size = "lg",
}: LoadingSpinnerProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-4",
        className,
      )}
    >
      <Loader2
        className={cn("animate-spin text-primary", sizeClasses[size])}
      />
      {message && (
        <p className="text-muted-foreground text-sm">{message}</p>
      )}
    </div>
  )
}

interface LoadingOverlayProps {
  message?: string
}

export function LoadingOverlay({
  message = "Carregando...",
}: LoadingOverlayProps) {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <LoadingSpinner message={message} />
    </div>
  )
}
