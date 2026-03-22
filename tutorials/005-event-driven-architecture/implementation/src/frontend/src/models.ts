export type SeedUser = {
  userId: string
  email: string
  displayName: string
  role: string
}

export type CurrentUser = SeedUser

export type RegisterAssetRequest = {
  assetKey: string
  title: string
  simulateFailure: boolean
}

export type AssetSummary = {
  assetId: string
  assetKey: string
  title: string
  lifecycleState: string
  scanStatus: string
  metadataStatus: string
  thumbnailStatus: string
  transcodeStatus: string
  failureReason: string | null
  updatedAt: string
}

export type AssetDetail = {
  assetId: string
  assetKey: string
  title: string
  lifecycleState: string
  scanStatus: string
  metadataStatus: string
  thumbnailStatus: string
  transcodeStatus: string
  simulateFailure: boolean
  submittedBy: string
  createdAt: string
  updatedAt: string
  readyAt: string | null
  failureReason: string | null
}

export type DashboardSummary = {
  totalAssets: number
  processingAssets: number
  readyAssets: number
  failedAssets: number
  pendingUploads: number
  notificationsSent: number
}

export type Notification = {
  notificationId: string
  assetId: string
  assetTitle: string
  message: string
  sentAt: string
}
