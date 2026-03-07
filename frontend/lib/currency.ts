/**
 * Formats a number as Brazilian Real (BRL) currency.
 */
export function formatCurrency(value: number): string {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value)
}

/**
 * Parses a currency input string that may use Brazilian format (1.234,56)
 * or standard format (1234.56). Returns NaN if unparseable.
 */
export function parseCurrencyInput(value: string): number {
  if (!value || !value.trim()) return NaN
  const trimmed = value.trim()
  const lastDot = trimmed.lastIndexOf(".")
  const lastComma = trimmed.lastIndexOf(",")
  let normalized: string
  if (lastComma > lastDot) {
    normalized = trimmed.replace(/\./g, "").replace(",", ".")
  } else if (lastDot > lastComma) {
    normalized = trimmed.replace(/,/g, "")
  } else {
    normalized = trimmed.replace(",", ".")
  }
  const result = Number(normalized)
  return isNaN(result) ? NaN : result
}
