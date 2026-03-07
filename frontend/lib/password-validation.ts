import { z } from "zod"

export const passwordRules = z
  .string()
  .min(8, "Senha deve ter pelo menos 8 caracteres")
  .regex(/[A-Z]/, "Senha deve conter ao menos uma letra maiúscula")
  .regex(/[a-z]/, "Senha deve conter ao menos uma letra minúscula")
  .regex(/[0-9]/, "Senha deve conter ao menos um número")
  .regex(/[^a-zA-Z0-9]/, "Senha deve conter ao menos um caractere especial")

export interface PasswordStrengthRule {
  label: string
  test: (value: string) => boolean
}

export const passwordStrengthRules: PasswordStrengthRule[] = [
  { label: "Pelo menos 8 caracteres", test: (v) => v.length >= 8 },
  { label: "Uma letra maiúscula", test: (v) => /[A-Z]/.test(v) },
  { label: "Uma letra minúscula", test: (v) => /[a-z]/.test(v) },
  { label: "Um número", test: (v) => /[0-9]/.test(v) },
  { label: "Um caractere especial", test: (v) => /[^a-zA-Z0-9]/.test(v) },
]

export function getPasswordStrength(password: string): number {
  if (!password) return 0
  return passwordStrengthRules.filter((rule) => rule.test(password)).length
}

export function getPasswordStrengthLabel(strength: number): string {
  if (strength === 0) return ""
  if (strength <= 2) return "Fraca"
  if (strength <= 3) return "Razoável"
  if (strength <= 4) return "Boa"
  return "Forte"
}

export function getPasswordStrengthColor(strength: number): string {
  if (strength === 0) return ""
  if (strength <= 2) return "text-destructive"
  if (strength <= 3) return "text-orange-500"
  if (strength <= 4) return "text-yellow-500"
  return "text-green-500"
}

export function getPasswordStrengthBarColor(strength: number): string {
  if (strength === 0) return "bg-muted"
  if (strength <= 2) return "bg-destructive"
  if (strength <= 3) return "bg-orange-500"
  if (strength <= 4) return "bg-yellow-500"
  return "bg-green-500"
}
