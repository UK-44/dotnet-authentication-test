<script setup lang="ts">
import { onMounted, ref } from 'vue'
import {
  deletePasskey,
  fetchPasskeys,
  isPasskeySupported,
  registerPasskey,
  renamePasskey,
  type Passkey,
} from '../api/passkey'

const passkeys = ref<Passkey[]>([])
const error = ref('')
const loading = ref(false)
const passkeySupported = isPasskeySupported()

// 追加はパスワードの再入力が必要
const adding = ref(false)
const password = ref('')

const editingId = ref<number | null>(null)
const editingName = ref('')

async function run(fn: () => Promise<void>) {
  error.value = ''
  loading.value = true
  try {
    await fn()
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

const load = () => run(async () => {
  passkeys.value = await fetchPasskeys()
})

function add() {
  return run(async () => {
    await registerPasskey(password.value)
    adding.value = false
    password.value = ''
    passkeys.value = await fetchPasskeys()
  })
}

function startEdit(p: Passkey) {
  editingId.value = p.id
  editingName.value = p.name ?? ''
}

function saveName(p: Passkey) {
  return run(async () => {
    const updated = await renamePasskey(p.id, editingName.value)
    passkeys.value = passkeys.value.map((x) => (x.id === updated.id ? updated : x))
    editingId.value = null
  })
}

function remove(p: Passkey) {
  if (!confirm(`「${p.name ?? 'パスキー'}」を削除しますか？`)) return
  return run(async () => {
    await deletePasskey(p.id)
    passkeys.value = await fetchPasskeys()
  })
}

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString('ja-JP', { dateStyle: 'medium', timeStyle: 'short' }) : '未使用'
}

onMounted(load)
</script>

<template>
  <section class="card">
    <h2>パスキー</h2>
    <p class="hint-text">パスキーを登録すると、パスワードを入力せずに、指紋・顔認証・PINでログインできます。</p>

    <ul v-if="passkeys.length" class="passkeys">
      <li v-for="p in passkeys" :key="p.id">
        <form v-if="editingId === p.id" class="rename" @submit.prevent="saveName(p)">
          <input v-model="editingName" required maxlength="64" aria-label="パスキーの名前" />
          <button type="submit" :disabled="loading">保存</button>
          <button type="button" class="secondary" @click="editingId = null">キャンセル</button>
        </form>
        <template v-else>
          <div class="passkey-info">
            <strong>{{ p.name ?? 'パスキー' }}</strong>
            <span class="meta">
              作成: {{ formatDate(p.createdAt) }} ／ 最終使用: {{ formatDate(p.lastUsedAt) }}
              <template v-if="p.isBackedUp"> ／ 同期済み</template>
            </span>
          </div>
          <div class="actions">
            <button type="button" class="secondary" @click="startEdit(p)">名前を変更</button>
            <button type="button" class="secondary danger" :disabled="loading" @click="remove(p)">削除</button>
          </div>
        </template>
      </li>
    </ul>
    <p v-else class="meta">登録済みのパスキーはありません</p>

    <p v-if="error" class="error">{{ error }}</p>

    <template v-if="passkeySupported">
      <form v-if="adding" class="add" @submit.prevent="add">
        <label>
          確認のため、パスワードを入力してください
          <input v-model="password" type="password" autocomplete="current-password" required />
        </label>
        <div class="actions">
          <button type="submit" :disabled="loading">{{ loading ? '登録中…' : 'パスキーを作成' }}</button>
          <button type="button" class="secondary" @click="adding = false">キャンセル</button>
        </div>
      </form>
      <button v-else type="button" @click="adding = true">パスキーを追加</button>
    </template>
    <p v-else class="meta">このブラウザはパスキーに対応していません</p>
  </section>
</template>
