import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'
import { VitePWA } from 'vite-plugin-pwa'

const vendorChunks: Record<string, string> = {
  'react/': 'react',
  'react-dom/': 'react',
  'react-router': 'react',
  '@tanstack/': 'query',
  '@radix-ui/': 'radix',
  'react-hook-form': 'forms',
  '@hookform/': 'forms',
  zod: 'forms',
}

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'prompt',
      strategies: 'injectManifest',
      srcDir: 'src',
      filename: 'sw.ts',
      manifest: {
        name: 'QL Đơn Hàng',
        short_name: 'QLĐơnHàng',
        description: 'Quản lý báo giá, khách hàng, hàng hóa',
        display: 'standalone',
        theme_color: '#1e40af',
        background_color: '#ffffff',
        start_url: '/',
        icons: [
          { src: '/api/settings/branding/icon/192', sizes: '192x192', type: 'image/png' },
          { src: '/api/settings/branding/icon/512', sizes: '512x512', type: 'image/png' },
        ],
      },
      devOptions: {
        enabled: true,
        type: 'module',
      },
    }),
  ],
  resolve: { alias: { '@': path.resolve(__dirname, './src') } },
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5050', changeOrigin: true },
      '/hubs': { target: 'http://localhost:5050', changeOrigin: true, ws: true },
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: (id) => {
          if (!id.includes('node_modules')) return undefined
          for (const [needle, chunk] of Object.entries(vendorChunks)) {
            if (id.includes(needle)) return chunk
          }
          return undefined
        },
      },
    },
  },
})
