import type {
  AssetDetail,
  AssetSummary,
  CurrentUser,
  DashboardSummary,
  Notification,
  RegisterAssetRequest,
  SeedUser,
} from './models'

async function request<T>(input: string, init?: RequestInit): Promise<T> {
  const response = await fetch(input, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
    ...init,
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `Request failed with status ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const api = {
  getSeedUsers: () => request<SeedUser[]>('/api/auth/seed-users'),
  login: (email: string, password: string) =>
    request<CurrentUser>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  logout: () =>
    request<void>('/api/auth/logout', {
      method: 'POST',
    }),
  getCurrentUser: () => request<CurrentUser>('/api/auth/me'),
  getDashboard: () => request<DashboardSummary>('/api/dashboard'),
  getAssets: () => request<AssetSummary[]>('/api/assets'),
  getAsset: (assetId: string) => request<AssetDetail>(`/api/assets/${assetId}`),
  createAsset: (payload: RegisterAssetRequest) =>
    request<AssetDetail>('/api/assets', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  markUploadComplete: (assetId: string) =>
    request<AssetDetail>(`/api/assets/${assetId}/upload-complete`, {
      method: 'POST',
    }),
  getNotifications: () => request<Notification[]>('/api/notifications'),
}
