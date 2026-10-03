import { createRouter, createWebHistory } from 'vue-router'
import { currentUser, fetchMe } from './api/auth'
import HomeView from './views/HomeView.vue'
import LoginView from './views/LoginView.vue'
import MyPageView from './views/MyPageView.vue'

let checked = false

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/mypage', component: MyPageView, meta: { requiresAuth: true } },
    { path: '/login', component: LoginView, meta: { guestOnly: true } },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

router.beforeEach(async (to) => {
  // 初回遷移時にセッションが有効か確認
  if (!checked) {
    await fetchMe()
    checked = true
  }
  if (to.meta.requiresAuth && !currentUser.value) {
    return { path: '/login', query: { redirect: to.fullPath } }
  }
  if (to.meta.guestOnly && currentUser.value) {
    return '/'
  }
})

export default router
