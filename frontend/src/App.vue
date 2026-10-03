<script setup lang="ts">
import { useRouter } from 'vue-router'
import { currentUser, logout } from './api/auth'

const router = useRouter()

async function onLogout() {
  await logout()
  router.push('/login')
}
</script>

<template>
  <header>
    <div class="nav">
      <h1>ログインアプリ</h1>
      <RouterLink to="/">トップ</RouterLink>
      <RouterLink v-if="currentUser" to="/mypage">マイページ</RouterLink>
    </div>
    <div v-if="currentUser" class="user">
      <span>{{ currentUser.userName }}</span>
      <button class="secondary" @click="onLogout">ログアウト</button>
    </div>
    <RouterLink v-else to="/login" class="secondary">ログイン</RouterLink>
  </header>
  <main>
    <RouterView />
  </main>
</template>
