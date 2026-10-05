import { currentUser, request, type User } from './auth'

export type Passkey = {
  id: number
  credentialId: string
  name: string | null
  isBackedUp: boolean
  createdAt: string
  lastUsedAt: string | null
}

type PasskeyList = { rpId: string; userHandle: string | null; passkeys: Passkey[] }

// オプションの JSON 変換はブラウザ標準の API に任せる
export function isPasskeySupported() {
  return (
    typeof PublicKeyCredential !== 'undefined' &&
    'parseCreationOptionsFromJSON' in PublicKeyCredential &&
    'parseRequestOptionsFromJSON' in PublicKeyCredential
  )
}

export async function fetchPasskeys() {
  const list = await request<PasskeyList>('/passkeys')
  // RP 側で削除したパスキーを、対応ブラウザではログイン時の選択画面から消してもらう
  if (list.userHandle && 'signalAllAcceptedCredentials' in PublicKeyCredential) {
    await PublicKeyCredential.signalAllAcceptedCredentials({
      rpId: list.rpId,
      userId: list.userHandle,
      allAcceptedCredentialIds: list.passkeys.map((p) => p.credentialId),
    }).catch(() => {})
  }
  return list.passkeys
}

export async function registerPasskey(password: string) {
  const options = await request<PublicKeyCredentialCreationOptionsJSON>('/passkeys/register/options', {
    method: 'POST',
    body: JSON.stringify({ password }),
  })
  const credential = await callWebAuthn(() =>
    navigator.credentials.create({ publicKey: PublicKeyCredential.parseCreationOptionsFromJSON(options) }),
  )
  return request<Passkey>('/passkeys/register', {
    method: 'POST',
    body: JSON.stringify(credential.toJSON()),
  })
}

export async function loginWithPasskey() {
  const options = await request<PublicKeyCredentialRequestOptionsJSON>('/passkeys/login/options', {
    method: 'POST',
  })
  const credential = await callWebAuthn(() =>
    navigator.credentials.get({ publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(options) }),
  )
  currentUser.value = await request<User>('/passkeys/login', {
    method: 'POST',
    body: JSON.stringify(credential.toJSON()),
  })
}

export async function renamePasskey(id: number, name: string) {
  return request<Passkey>(`/passkeys/${id}`, { method: 'PATCH', body: JSON.stringify({ name }) })
}

export async function deletePasskey(id: number) {
  await request<void>(`/passkeys/${id}`, { method: 'DELETE' })
}

async function callWebAuthn(fn: () => Promise<Credential | null>) {
  try {
    return (await fn()) as PublicKeyCredential
  } catch (e) {
    if (e instanceof DOMException) {
      // キャンセル・タイムアウト・使えるパスキーがない場合は、どれも NotAllowedError になる
      if (e.name === 'NotAllowedError') throw new Error('パスキーの操作がキャンセルされたか、タイムアウトしました')
      // excludeCredentials に含まれるパスキーを、認証器がすでに持っている
      if (e.name === 'InvalidStateError') throw new Error('この認証器にはすでにパスキーが登録されています')
    }
    throw e
  }
}
