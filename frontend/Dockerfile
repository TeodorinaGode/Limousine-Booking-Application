# ---- Base: shared dependency install, used by both the dev and build stages ----
FROM node:20-alpine AS base
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

# ---- Dev: Vite dev server with HMR — this is what docker-compose.yml uses for
# local development (`target: dev`), unchanged from before this file was split
# into stages. Source is bind-mounted over this at runtime; the COPY below just
# makes `docker build --target dev` usable standalone too. ----
FROM base AS dev
COPY . .
EXPOSE 5173
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]

# ---- Build: compiles the production bundle. VITE_* variables are inlined into
# the JS bundle at build time (there's no server-side runtime to read them from
# later), so they're accepted as build args here rather than as container env
# vars — pass them via `docker build --build-arg VITE_API_BASE_URL=... .` or
# your hosting platform's build-arg equivalent. ----
FROM base AS build
COPY . .
ARG VITE_API_BASE_URL
ARG VITE_MAP_TILE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL \
    VITE_MAP_TILE_URL=$VITE_MAP_TILE_URL
RUN npm run build

# ---- Production: nginx serving the static build. Last stage in the file, so
# it's the default target — a plain `docker build .` (no --target) produces
# this, not the dev server. ----
FROM nginx:alpine AS production
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
