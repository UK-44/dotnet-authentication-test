<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { login, register } from '../api/auth'

const route = useRoute()
const router = useRouter()

const mode = ref<'login' | 'register'>('login')
const userName = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    if (mode.value === 'login') {
      await login(userName.value, password.value)
    } else {
      await register(userName.value, password.value)
    }
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    router.push(redirect.startsWith('/') ? redirect : '/')
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

function toggle() {
  mode.value = mode.value === 'login' ? 'register' : 'login'
  error.value = ''
}
</script>

<template>
  <form class="card" @submit.prevent="submit">
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
    <button type="button" class="link" @click="toggle">
      {{ mode === 'login' ? 'アカウントを作成する' : 'ログイン画面に戻る' }}
    </button>
  </form>
</template>
