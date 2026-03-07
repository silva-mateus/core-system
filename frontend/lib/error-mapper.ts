import type { ProblemDetails } from "@core/types/problem-details"

const defaultStatusMessages: Record<number, string> = {
  401: "Sessão expirada. Faça login novamente.",
  403: "Você não tem permissão para esta ação.",
  404: "Recurso não encontrado.",
  429: "Muitas tentativas. Aguarde um momento.",
  500: "Erro interno do servidor.",
}

/**
 * Creates an error mapper function with app-specific error code messages.
 * The returned function extracts error info from API responses (ProblemDetails or plain objects)
 * and maps them to pt-BR user-facing messages.
 *
 * @example
 * const getErrorMessage = createErrorMapper({
 *   DUPLICATE_CATEGORY: "Já existe uma categoria com este nome.",
 *   FILE_NOT_FOUND: "Arquivo não encontrado.",
 * })
 */
export function createErrorMapper(
  appErrorCodes: Record<string, string> = {},
): (error: unknown) => string {
  const errorCodeMessages: Record<string, string> = {
    FORBIDDEN: "Você não tem permissão para esta ação.",
    VALIDATION_ERROR: "Dados inválidos. Verifique os campos.",
    INTERNAL_ERROR: "Erro interno. Tente novamente mais tarde.",
    NOT_FOUND: "Recurso não encontrado.",
    CONFLICT: "Conflito detectado.",
    BUSINESS_RULE_VIOLATION: "Operação não permitida.",
    ...appErrorCodes,
  }

  return (error: unknown): string => {
    const err = error as {
      response?: { data?: Record<string, unknown>; status?: number }
      code?: string
    }

    if (err?.response?.data) {
      const data = err.response.data as ProblemDetails & Record<string, unknown>
      const resolvedErrorCode = (data.errorCode || data.code) as
        | string
        | undefined
      const resolvedDetail = (data.detail || data.error) as string | undefined

      if (resolvedErrorCode && errorCodeMessages[resolvedErrorCode]) {
        return errorCodeMessages[resolvedErrorCode]
      }

      if (resolvedDetail && typeof resolvedDetail === "string") {
        return resolvedDetail
      }

      const validationErrors = data.errors as
        | Record<string, string[]>
        | undefined
      if (validationErrors) {
        const firstField = Object.keys(validationErrors)[0]
        if (firstField && validationErrors[firstField].length > 0) {
          return validationErrors[firstField][0]
        }
      }
    }

    if (err?.response?.status) {
      const statusMsg = defaultStatusMessages[err.response.status]
      if (statusMsg) return statusMsg
    }

    if (err?.code === "ERR_NETWORK") {
      return "Erro de conexão. Verifique sua internet."
    }

    if (error instanceof Error && error.message) {
      return error.message
    }

    return "Ocorreu um erro inesperado."
  }
}

/**
 * Default error mapper with no app-specific codes.
 * Apps should create their own via createErrorMapper() for custom error codes.
 */
export const getErrorMessage = createErrorMapper()
