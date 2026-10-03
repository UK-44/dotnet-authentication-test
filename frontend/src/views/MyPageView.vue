<script setup lang="ts">
import { currentUser } from '../api/auth'
</script>

<template>
  <section class="card">
    <h2>マイページ</h2>
    <dl>
      <dt>ユーザー名</dt>
      <dd>{{ currentUser?.userName }}</dd>
      <dt>ID</dt>
      <dd>{{ currentUser?.id }}</dd>
    </dl>
  </section>
</template>
