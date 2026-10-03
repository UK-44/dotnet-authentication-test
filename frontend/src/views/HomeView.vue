<script setup lang="ts">
import { currentUser } from '../api/auth'
</script>

<template>
  <section class="card">
    <h2>ようこそ、{{ currentUser?.userName }} さん</h2>
    <p>ログインに成功しました。</p>
  </section>
</template>
