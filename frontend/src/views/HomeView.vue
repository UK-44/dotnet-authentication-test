<script setup lang="ts">
import { currentUser } from '../api/auth'
</script>

<template>
  <section class="card">
    <h2>トップ</h2>
    <p>{{ currentUser ? 'あなたはログイン中です' : 'あなたは未ログインです' }}</p>
  </section>
</template>
