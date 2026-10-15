import { defineConfig } from 'astro/config';

// Static site: `npm run build` writes plain HTML/CSS/images to dist/.
// The Dockerfile builds it and serves dist/ with nginx.
export default defineConfig({
  site: process.env.SITE_URL || 'https://www.firstpvrobotics.com',
  trailingSlash: 'ignore',
  build: { format: 'directory' },
});
