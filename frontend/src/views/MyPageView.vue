<script setup lang="ts">
import { currentUser } from '../api/auth'
</script>

<template>
  <section class="card">
    <h2>マイページ</h2>
    <dl class="info">
      <div>
        <dt>ユーザー名</dt>
        <dd>{{ currentUser?.userName }}</dd>
      </div>
      <div>
        <dt>ID</dt>
        <dd>{{ currentUser?.id }}</dd>
      </div>
    </dl>
  </section>
</template>
