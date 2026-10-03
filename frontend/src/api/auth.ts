import { ref } from 'vue'

export type User = { id: number; userName: string }

export const currentUser = ref<User | null>(null)

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`/api${path}`, {
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })
  if (!res.ok) {
    const body = await res.json().catch(() => null)
    const message =
      body?.message ??
      (body?.errors ? Object.values(body.errors).flat().join('\n') : null) ??
      `エラーが発生しました (${res.status})`
    throw Object.assign(new Error(message), { status: res.status })
  }
  return res.status === 204 ? (undefined as T) : res.json()
}

export async function fetchMe() {
  try {
    currentUser.value = await request<User>('/auth/me')
  } catch {
    currentUser.value = null
  }
  return currentUser.value
}

export async function login(userName: string, password: string) {
  currentUser.value = await request<User>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  })
}

export async function register(userName: string, password: string) {
  currentUser.value = await request<User>('/auth/register', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  })
}

export async function logout() {
  await request<void>('/auth/logout', { method: 'POST' })
  currentUser.value = null
}
