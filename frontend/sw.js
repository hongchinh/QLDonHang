import { precacheAndRoute, cleanupOutdatedCaches } from 'workbox-precaching'

precacheAndRoute([{"revision":"55ef8728341da6ea243a4f977170bbf4","url":"sw.js"},{"revision":"1872c500de691dce40960bb85481de07","url":"registerSW.js"},{"revision":"01d2b76aaf962b2d7a09b91212fd5c55","url":"index.html"},{"revision":"4e9f2ef684c5304b1dabfa62c1b4ca3a","url":"icons/icon-512.png"},{"revision":"2d513c8ec1c0f9b4cdd3fc6aab25d860","url":"icons/icon-192.png"},{"revision":null,"url":"assets/react-B40owc3x.js"},{"revision":null,"url":"assets/radix-W2fh30mY.js"},{"revision":null,"url":"assets/query-CWM969zv.js"},{"revision":null,"url":"assets/index-D4ighs5h.js"},{"revision":null,"url":"assets/index-BG5B8CRj.css"},{"revision":null,"url":"assets/forms-CwSfMVhy.js"},{"revision":"2d513c8ec1c0f9b4cdd3fc6aab25d860","url":"icons/icon-192.png"},{"revision":"4e9f2ef684c5304b1dabfa62c1b4ca3a","url":"icons/icon-512.png"},{"revision":"7b83c22d0549d9fa54d9b3ccf047041c","url":"manifest.webmanifest"}])
cleanupOutdatedCaches()

self.addEventListener('message', (event) => {
  if (event.data?.type === 'SKIP_WAITING') {
    self.skipWaiting()
  }
})
