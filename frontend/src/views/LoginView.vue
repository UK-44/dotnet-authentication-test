<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { login, register } from '../api/auth'
import { fetchPasskeys, isPasskeySupported, loginWithPasskey, registerPasskey } from '../api/passkey'

const route = useRoute()
const router = useRouter()

const mode = ref<'login' | 'register' | 'prompt'>('login')
const userName = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const passkeySupported = isPasskeySupported()

function goNext() {
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
  router.push(redirect.startsWith('/') ? redirect : '/')
}

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

function submit() {
  return run(async () => {
    if (mode.value === 'login') {
      await login(userName.value, password.value)
    } else {
      await register(userName.value, password.value)
    }
    // パスワードでログインした直後に、パスキーが未登録なら登録を案内する
    if (passkeySupported && (await fetchPasskeys()).length === 0) {
      mode.value = 'prompt'
      return
    }
    goNext()
  })
}

function submitPasskey() {
  return run(async () => {
    await loginWithPasskey()
    goNext()
  })
}

// 直前に入力されたパスワードを、パスキー追加前の再認証にそのまま使う
function registerFromPrompt() {
  return run(async () => {
    await registerPasskey(password.value)
    goNext()
  })
}

function toggle() {
  mode.value = mode.value === 'login' ? 'register' : 'login'
  error.value = ''
}
</script>

<template>
  <section v-if="mode === 'prompt'" class="card">
    <h2>パスキーを登録しませんか？</h2>
    <p>次回から、パスワードを入力せずに、指紋・顔認証・PINでログインできます。</p>
    <p v-if="error" class="error">{{ error }}</p>
    <button type="button" :disabled="loading" @click="registerFromPrompt">
      {{ loading ? '登録中…' : 'パスキーを登録する' }}
    </button>
    <button type="button" class="link" @click="goNext">あとで</button>
  </section>

  <form v-else class="card" @submit.prevent="submit">
    <h2>{{ mode === 'login' ? 'ログイン' : '新規登録' }}</h2>
    <label>
      ユーザー名
      <input v-model="userName" autocomplete="username" required minlength="3" maxlength="32" />
    </label>
    <label>
      パスワード
      <input
        v-model="password"
        type="password"
        :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
        required
        :minlength="mode === 'register' ? 8 : undefined"
      />
    </label>
    <p v-if="mode === 'register'" class="hint">パスワードは 8 文字以上</p>
    <p v-if="error" class="error">{{ error }}</p>
    <button type="submit" :disabled="loading">
      {{ loading ? '送信中…' : mode === 'login' ? 'ログイン' : '登録する' }}
    </button>
    <template v-if="mode === 'login' && passkeySupported">
      <div class="divider">または</div>
      <button type="button" class="secondary-large" :disabled="loading" @click="submitPasskey">
        パスキーでログイン
      </button>
    </template>
    <button type="button" class="link" @click="toggle">
      {{ mode === 'login' ? 'アカウントを作成する' : 'ログイン画面に戻る' }}
    </button>
  </form>
</template>
