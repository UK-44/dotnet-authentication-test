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
    <h1>ログインアプリ</h1>
    <div v-if="currentUser" class="user">
      <span>{{ currentUser.userName }}</span>
      <button class="secondary" @click="onLogout">ログアウト</button>
    </div>
  </header>
  <main>
    <RouterView />
  </main>
</template>
