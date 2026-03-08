export async function fetchJson<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
  const response = await fetch(input, init)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
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

  return `Request failed with status ${response.status}`
}
