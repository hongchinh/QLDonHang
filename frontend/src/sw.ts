import { precacheAndRoute, cleanupOutdatedCaches } from 'workbox-precaching'

declare const self: any

precacheAndRoute(self.__WB_MANIFEST)
cleanupOutdatedCaches()

self.addEventListener('message', (event: any) => {
  if (event.data?.type === 'SKIP_WAITING') {
    self.skipWaiting()
  }
})
