"use client"

import { Check, X } from "lucide-react"
import {
  passwordStrengthRules,
  getPasswordStrength,
  getPasswordStrengthLabel,
  getPasswordStrengthColor,
  getPasswordStrengthBarColor,
} from "@core/lib/password-validation"

interface PasswordStrengthIndicatorProps {
  password: string
}

export function PasswordStrengthIndicator({
  password,
}: PasswordStrengthIndicatorProps) {
  const strength = getPasswordStrength(password)
  const label = getPasswordStrengthLabel(strength)
  const colorClass = getPasswordStrengthColor(strength)
  const barColor = getPasswordStrengthBarColor(strength)

  if (!password) return null

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2">
        <div className="flex-1 flex gap-1">
          {Array.from({ length: 5 }).map((_, i) => (
            <div
              key={i}
              className={`h-1.5 flex-1 rounded-full transition-colors ${
                i < strength ? barColor : "bg-muted"
              }`}
            />
          ))}
        </div>
        {label && (
          <span className={`text-xs font-medium ${colorClass}`}>{label}</span>
        )}
      </div>

      <ul className="space-y-1">
        {passwordStrengthRules.map((rule) => {
          const passed = rule.test(password)
          return (
            <li key={rule.label} className="flex items-center gap-1.5 text-xs">
              {passed ? (
                <Check className="h-3 w-3 text-green-500" />
              ) : (
                <X className="h-3 w-3 text-muted-foreground" />
              )}
              <span
                className={
                  passed
                    ? "text-green-600 dark:text-green-400"
                    : "text-muted-foreground"
                }
              >
                {rule.label}
              </span>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
