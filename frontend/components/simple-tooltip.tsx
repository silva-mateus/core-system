"use client"

import {
  Tooltip,
  TooltipTrigger,
  TooltipContent,
} from "@core/components/ui/tooltip"

interface SimpleTooltipProps {
  children: React.ReactNode
  label: string
  side?: "top" | "bottom" | "left" | "right"
}

export function SimpleTooltip({
  children,
  label,
  side = "top",
}: SimpleTooltipProps) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>{children}</TooltipTrigger>
      <TooltipContent side={side}>
        <p>{label}</p>
      </TooltipContent>
    </Tooltip>
  )
}
