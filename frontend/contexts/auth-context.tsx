"use client"

import React, { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from "react"
import { createApiClient, type ApiClient } from "@core/lib/api-client"
import { getFromStorage, setToStorage, removeFromStorage } from "@core/lib/storage"

export interface CoreUser {
  id: number
  username: string
  fullName: string
  role: string | null
  mustChangePassword: boolean
}

export interface CoreAuthConfig {
  apiBasePath?: string
  storagePrefix?: string
  onLogout?: () => void
  onSessionExpired?: () => void
}

interface AuthState {
  user: CoreUser | null
  permissions: string[]
  isLoading: boolean
  isAuthenticated: boolean
}

interface AuthContextValue extends AuthState {
  login: (username: string, password: string) => Promise<{ mustChangePassword: boolean }>
  logout: () => Promise<void>
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>
  updateProfile: (fullName: string) => Promise<void>
  refreshUser: () => Promise<void>
  hasPermission: (key: string) => boolean
  api: ApiClient
}

const AuthContext = createContext<AuthContextValue | null>(null)

interface ApiUserResponse {
  id: number
  username: string
  full_name?: string
  fullName?: string
  role: string | null
  must_change_password?: boolean
  mustChangePassword?: boolean
}

function mapUser(raw: ApiUserResponse): CoreUser {
  return {
    id: raw.id,
    username: raw.username,
    fullName: raw.full_name ?? raw.fullName ?? "",
    role: raw.role,
    mustChangePassword: raw.must_change_password ?? raw.mustChangePassword ?? false,
  }
}

interface AuthProviderProps {
  children: ReactNode
  config?: CoreAuthConfig
}

export function CoreAuthProvider({ children, config = {} }: AuthProviderProps) {
  const {
    apiBasePath = "/api",
    storagePrefix = "core_auth",
    onLogout,
    onSessionExpired,
  } = config

  const api = React.useMemo(
    () => createApiClient({ baseUrl: apiBasePath, onUnauthorized: onSessionExpired }),
    [apiBasePath, onSessionExpired]
  )

  const [state, setState] = useState<AuthState>({
    user: null,
    permissions: [],
    isLoading: true,
    isAuthenticated: false,
  })

  const refreshUser = useCallback(async () => {
    try {
      const data = await api.get<{ user: ApiUserResponse; permissions: string[] }>("/auth/me")
      const user = mapUser(data.user)
      setState({
        user,
        permissions: data.permissions,
        isLoading: false,
        isAuthenticated: true,
      })
      setToStorage(`${storagePrefix}_user`, user)
      setToStorage(`${storagePrefix}_permissions`, data.permissions)
    } catch {
      setState({ user: null, permissions: [], isLoading: false, isAuthenticated: false })
      removeFromStorage(`${storagePrefix}_user`)
      removeFromStorage(`${storagePrefix}_permissions`)
    }
  }, [api, storagePrefix])

  useEffect(() => {
    const cachedUser = getFromStorage<CoreUser | null>(`${storagePrefix}_user`, null)
    const cachedPermissions = getFromStorage<string[]>(`${storagePrefix}_permissions`, [])
    if (cachedUser) {
      setState({
        user: cachedUser,
        permissions: cachedPermissions,
        isLoading: true,
        isAuthenticated: true,
      })
    }
    refreshUser()
  }, [refreshUser, storagePrefix])

  const login = useCallback(
    async (username: string, password: string) => {
      const data = await api.post<{ user: ApiUserResponse; permissions: string[] }>("/auth/login", {
        username,
        password,
      })
      const user = mapUser(data.user)
      setState({
        user,
        permissions: data.permissions,
        isLoading: false,
        isAuthenticated: true,
      })
      setToStorage(`${storagePrefix}_user`, user)
      setToStorage(`${storagePrefix}_permissions`, data.permissions)
      return { mustChangePassword: user.mustChangePassword }
    },
    [api, storagePrefix]
  )

  const logout = useCallback(async () => {
    try {
      await api.post("/auth/logout")
    } catch {
      // even if the API call fails, clear local state
    }
    setState({ user: null, permissions: [], isLoading: false, isAuthenticated: false })
    removeFromStorage(`${storagePrefix}_user`)
    removeFromStorage(`${storagePrefix}_permissions`)
    onLogout?.()
  }, [api, storagePrefix, onLogout])

  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      await api.post("/auth/change-password", { current_password: currentPassword, new_password: newPassword })
      await refreshUser()
    },
    [api, refreshUser]
  )

  const updateProfile = useCallback(
    async (fullName: string) => {
      await api.put("/auth/profile", { full_name: fullName })
      await refreshUser()
    },
    [api, refreshUser]
  )

  const hasPermission = useCallback(
    (key: string) => state.permissions.includes(key),
    [state.permissions]
  )

  const value: AuthContextValue = {
    ...state,
    login,
    logout,
    changePassword,
    updateProfile,
    refreshUser,
    hasPermission,
    api,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error("useAuth must be used within a CoreAuthProvider")
  }
  return context
}
