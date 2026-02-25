export interface ApiClientConfig {
  baseUrl?: string
  onUnauthorized?: () => void
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public statusText: string,
    public data?: unknown
  ) {
    super(`API Error ${status}: ${statusText}`)
    this.name = "ApiError"
  }
}

export function createApiClient(config: ApiClientConfig = {}) {
  const { baseUrl = "/api", onUnauthorized } = config

  async function request<T>(
    path: string,
    init?: RequestInit & { params?: Record<string, string> }
  ): Promise<T> {
    const { params, ...fetchInit } = init ?? {}

    let url = `${baseUrl}${path}`
    if (params) {
      const searchParams = new URLSearchParams(params)
      url += `?${searchParams.toString()}`
    }

    const res = await fetch(url, {
      ...fetchInit,
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        ...fetchInit?.headers,
      },
    })

    if (res.status === 401) {
      onUnauthorized?.()
      throw new ApiError(res.status, res.statusText)
    }

    if (!res.ok) {
      let data: unknown
      try {
        data = await res.json()
      } catch {
        // no json body
      }
      throw new ApiError(res.status, res.statusText, data)
    }

    if (res.status === 204) return undefined as T

    return res.json() as Promise<T>
  }

  return {
    get: <T>(path: string, params?: Record<string, string>) =>
      request<T>(path, { method: "GET", params }),

    post: <T>(path: string, body?: unknown) =>
      request<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined }),

    put: <T>(path: string, body?: unknown) =>
      request<T>(path, { method: "PUT", body: body ? JSON.stringify(body) : undefined }),

    patch: <T>(path: string, body?: unknown) =>
      request<T>(path, { method: "PATCH", body: body ? JSON.stringify(body) : undefined }),

    delete: <T>(path: string) =>
      request<T>(path, { method: "DELETE" }),

    upload: <T>(path: string, formData: FormData) =>
      request<T>(path, {
        method: "POST",
        body: formData,
        headers: {},
      }),

    request,
  }
}

export type ApiClient = ReturnType<typeof createApiClient>
