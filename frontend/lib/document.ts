/**
 * Strips all non-digit characters from a document string.
 */
export function stripDocumentFormatting(doc: string): string {
  return doc.replace(/\D/g, "")
}

/**
 * Formats a CPF (999.999.999-99) or CNPJ (99.999.999/9999-99) for display.
 */
export function formatDocument(doc: string | null | undefined): string {
  if (!doc) return ""
  const digits = stripDocumentFormatting(doc)
  if (digits.length === 11) {
    return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`
  }
  if (digits.length === 14) {
    return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}/${digits.slice(8, 12)}-${digits.slice(12)}`
  }
  return doc
}

/**
 * Validates a CPF using check digit algorithm.
 */
export function isValidCpf(cpf: string): boolean {
  const digits = stripDocumentFormatting(cpf)
  if (digits.length !== 11) return false
  if (/^(\d)\1{10}$/.test(digits)) return false

  let sum = 0
  for (let i = 0; i < 9; i++) sum += Number(digits[i]) * (10 - i)
  let rem = sum % 11
  const first = rem < 2 ? 0 : 11 - rem
  if (Number(digits[9]) !== first) return false

  sum = 0
  for (let i = 0; i < 10; i++) sum += Number(digits[i]) * (11 - i)
  rem = sum % 11
  const second = rem < 2 ? 0 : 11 - rem
  return Number(digits[10]) === second
}

/**
 * Validates a CNPJ using check digit algorithm.
 */
export function isValidCnpj(cnpj: string): boolean {
  const digits = stripDocumentFormatting(cnpj)
  if (digits.length !== 14) return false
  if (/^(\d)\1{13}$/.test(digits)) return false

  const w1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
  const w2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]

  let sum = 0
  for (let i = 0; i < 12; i++) sum += Number(digits[i]) * w1[i]
  let rem = sum % 11
  const first = rem < 2 ? 0 : 11 - rem
  if (Number(digits[12]) !== first) return false

  sum = 0
  for (let i = 0; i < 13; i++) sum += Number(digits[i]) * w2[i]
  rem = sum % 11
  const second = rem < 2 ? 0 : 11 - rem
  return Number(digits[13]) === second
}

/**
 * Validates a document (auto-detects CPF or CNPJ by length).
 */
export function isValidDocument(doc: string): boolean {
  const digits = stripDocumentFormatting(doc)
  if (digits.length === 11) return isValidCpf(digits)
  if (digits.length === 14) return isValidCnpj(digits)
  return false
}

/**
 * Applies CPF/CNPJ input mask as user types.
 */
export function applyDocumentMask(value: string): string {
  const digits = stripDocumentFormatting(value).slice(0, 14)
  if (digits.length <= 11) {
    return digits
      .replace(/(\d{3})(\d)/, "$1.$2")
      .replace(/(\d{3})(\d)/, "$1.$2")
      .replace(/(\d{3})(\d{1,2})$/, "$1-$2")
  }
  return digits
    .replace(/(\d{2})(\d)/, "$1.$2")
    .replace(/(\d{3})(\d)/, "$1.$2")
    .replace(/(\d{3})(\d)/, "$1/$2")
    .replace(/(\d{4})(\d{1,2})$/, "$1-$2")
}
