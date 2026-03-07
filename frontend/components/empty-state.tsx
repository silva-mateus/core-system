"use client"

import { type LucideIcon } from "lucide-react"
import { Button } from "@core/components/ui/button"
import { cn } from "@core/lib/utils"

interface EmptyStateProps {
  icon?: LucideIcon
  title: string
  description?: string
  action?: {
    label: string
    onClick: () => void
    icon?: LucideIcon
  }
  secondaryAction?: {
    label: string
    onClick: () => void
    href?: string
  }
  className?: string
}

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
  secondaryAction,
  className,
}: EmptyStateProps) {
  return (
    <div className={cn("text-center py-12", className)}>
      <div className="flex flex-col items-center gap-4">
        {Icon && (
          <div className="p-4 bg-muted rounded-full">
            <Icon className="h-8 w-8 text-muted-foreground" />
          </div>
        )}
        <div className="space-y-2">
          <h3 className="text-lg font-medium">{title}</h3>
          {description && (
            <p className="text-muted-foreground max-w-sm mx-auto">
              {description}
            </p>
          )}
        </div>
        {(action || secondaryAction) && (
          <div className="flex gap-3 mt-2">
            {action && (
              <Button onClick={action.onClick} className="gap-2">
                {action.icon && <action.icon className="h-4 w-4" />}
                {action.label}
              </Button>
            )}
            {secondaryAction && (
              <Button variant="outline" onClick={secondaryAction.onClick}>
                {secondaryAction.label}
              </Button>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
