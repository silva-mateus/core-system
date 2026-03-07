"use client"

import { type LucideIcon } from "lucide-react"
import { cn } from "@core/lib/utils"

interface PageHeaderProps {
  icon?: LucideIcon
  title: string
  description?: string
  actions?: React.ReactNode
  children?: React.ReactNode
  className?: string
}

export function PageHeader({
  icon: Icon,
  title,
  description,
  actions,
  children,
  className,
}: PageHeaderProps) {
  const actionsContent = actions || children

  return (
    <div
      className={cn(
        "flex flex-col sm:flex-row sm:justify-between sm:items-start gap-4",
        className,
      )}
    >
      <div className="flex-1 min-w-0">
        <h1 className="text-2xl sm:text-3xl font-bold flex items-center gap-2">
          {Icon && (
            <Icon className="h-6 w-6 sm:h-8 sm:w-8 text-primary shrink-0" />
          )}
          <span className="truncate">{title}</span>
        </h1>
        {description && (
          <p className="text-muted-foreground mt-2 text-sm sm:text-base">
            {description}
          </p>
        )}
      </div>
      {actionsContent && (
        <div className="flex items-center gap-2 shrink-0">{actionsContent}</div>
      )}
    </div>
  )
}
