import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { api } from './api'

vi.mock('./api', () => ({
  api: {
    getSeedUsers: vi.fn(),
    login: vi.fn(),
    logout: vi.fn(),
    getCurrentUser: vi.fn(),
    getDashboard: vi.fn(),
    getAssets: vi.fn(),
    getAsset: vi.fn(),
    createAsset: vi.fn(),
    markUploadComplete: vi.fn(),
    getNotifications: vi.fn(),
  },
}))

const mockedApi = vi.mocked(api)

beforeEach(() => {
  vi.resetAllMocks()
})

describe('App', () => {
  it('shows the seeded login options', async () => {
    mockedApi.getSeedUsers.mockResolvedValue([
      {
        userId: '1',
        email: 'coordinator@eventdriven.local',
        displayName: 'Coordinator',
        role: 'ContentOperationsCoordinator',
      },
    ])
    mockedApi.getCurrentUser.mockRejectedValue(new Error('not signed in'))

    render(<App />)

    expect(await screen.findByText(/digital asset processing console/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /coordinator/i })).toBeInTheDocument()
  })

  it('renders the workspace after login and can register an asset', async () => {
    mockedApi.getSeedUsers.mockResolvedValue([
      {
        userId: '1',
        email: 'coordinator@eventdriven.local',
        displayName: 'Coordinator',
        role: 'ContentOperationsCoordinator',
      },
    ])
    mockedApi.getCurrentUser.mockRejectedValue(new Error('not signed in'))
    mockedApi.login.mockResolvedValue({
      userId: '1',
      email: 'coordinator@eventdriven.local',
      displayName: 'Coordinator',
      role: 'ContentOperationsCoordinator',
    })
    mockedApi.getDashboard.mockResolvedValue({
      totalAssets: 1,
      processingAssets: 0,
      readyAssets: 0,
      failedAssets: 0,
      pendingUploads: 1,
      notificationsSent: 0,
    })
    mockedApi.getAssets.mockResolvedValue([
      {
        assetId: 'asset-1',
        assetKey: 'ASSET-001',
        title: 'Launch trailer',
        lifecycleState: 'Registered',
        scanStatus: 'Pending',
        metadataStatus: 'Pending',
        thumbnailStatus: 'Pending',
        transcodeStatus: 'Pending',
        failureReason: null,
        updatedAt: new Date().toISOString(),
      },
    ])
    mockedApi.getAsset.mockResolvedValue({
      assetId: 'asset-1',
      assetKey: 'ASSET-001',
      title: 'Launch trailer',
      lifecycleState: 'Registered',
      scanStatus: 'Pending',
      metadataStatus: 'Pending',
      thumbnailStatus: 'Pending',
      transcodeStatus: 'Pending',
      simulateFailure: false,
      submittedBy: 'coordinator@eventdriven.local',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      readyAt: null,
      failureReason: null,
    })
    mockedApi.getNotifications.mockResolvedValue([])
    mockedApi.createAsset.mockResolvedValue({
      assetId: 'asset-2',
      assetKey: 'ASSET-002',
      title: 'New promo cut',
      lifecycleState: 'Registered',
      scanStatus: 'Pending',
      metadataStatus: 'Pending',
      thumbnailStatus: 'Pending',
      transcodeStatus: 'Pending',
      simulateFailure: false,
      submittedBy: 'coordinator@eventdriven.local',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      readyAt: null,
      failureReason: null,
    })

    render(<App />)

    fireEvent.click((await screen.findAllByRole('button', { name: /^sign in$/i }))[0])

    expect(await screen.findByText(/asset ingestion operations/i)).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText(/asset key/i), { target: { value: 'ASSET-002' } })
    fireEvent.change(screen.getByLabelText(/title/i), { target: { value: 'New promo cut' } })
    fireEvent.click(screen.getByRole('button', { name: /register asset/i }))

    await waitFor(() =>
      expect(mockedApi.createAsset).toHaveBeenCalledWith({
        assetKey: 'ASSET-002',
        title: 'New promo cut',
        simulateFailure: false,
      }),
    )
  })
})
