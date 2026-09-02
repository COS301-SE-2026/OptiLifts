FROM node:20-alpine AS build

RUN corepack enable 

WORKDIR /app

COPY pnpm-workspace.yaml package.json pnpm-lock.yaml ./
COPY frontend/package.json frontend/

RUN pnpm install --filter frontend --frozen-lockfile

COPY frontend/ ./frontend/

ARG VITE_API_BASE
ENV VITE_API_BASE=$VITE_API_BASE

ARG VITE_SENTRY_DSN
ENV VITE_SENTRY_DSN=$VITE_SENTRY_DSN

ARG SENTRY_RELEASE
ENV SENTRY_RELEASE=$SENTRY_RELEASE

ARG GOOGLE_CLIENT_ID
ENV GOOGLE_CLIENT_ID=$GOOGLE_CLIENT_ID

WORKDIR /app/frontend

RUN --mount=type=secret,id=sentry_auth_token \
    SENTRY_AUTH_TOKEN=$(cat /run/secrets/sentry_auth_token) pnpm run build

FROM nginxinc/nginx-unprivileged:1.25
ENV NGINX_BACKEND_URL=http://core-api:8080
ENV NGINX_AUTH_RATE_LIMIT=5r/s
ENV NGINX_GENERAL_RATE_LIMIT=30r/s
ENV NGINX_AUTH_BURST=10
ENV NGINX_GENERAL_BURST=50
COPY frontend/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/frontend/dist /usr/share/nginx/html

EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
