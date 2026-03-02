# ── Stage 1: Build ──────────────────────────────────────────
FROM node:20-alpine AS build
LABEL build.version="v6-whatsapp-2026-03-02"
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

# Force cache invalidation: RUN depends on CACHEBUST arg
ARG CACHEBUST=1
RUN echo "CACHEBUST=$CACHEBUST" > /tmp/.cachebust
COPY . .
RUN npm run build

# ── Stage 2: Serve with Nginx ──────────────────────────────
FROM nginx:alpine AS runtime

# Copy built files
COPY --from=build /app/dist /usr/share/nginx/html

# Nginx config for SPA routing + API reverse proxy
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
