export class ApiError extends Error {
  public readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}

export async function fetchJson<T>(input: RequestInfo | URL, init?: RequestInit): Promise<T> {
  const response = await fetch(input, {
    credentials: 'include',
    ...init,
  })

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response), response.status)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const payload = (await response.json()) as {
      message?: string
      title?: string
      errors?: Record<string, string[]>
    }

    if (payload.message) {
      return payload.message
    }

    if (payload.errors) {
      return Object.entries(payload.errors)
        .flatMap(([field, messages]) => messages.map((message) => `${field}: ${message}`))
        .join(' | ')
    }

    if (payload.title) {
      return payload.title
    }
  } catch {
    // Ignore parse failures and fall back to status text.
  }

  return response.status === 401
    ? 'Authentication is required.'
    : response.status === 403
      ? 'You do not have permission to perform this action.'
      : `Request failed with status ${response.status}`
}
