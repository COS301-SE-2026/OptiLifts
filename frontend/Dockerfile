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

WORKDIR /app/frontend

RUN --mount=type=secret,id=sentry_auth_token \
    SENTRY_AUTH_TOKEN=$(cat /run/secrets/sentry_auth_token) pnpm run build

WORKDIR /app/frontend
RUN pnpm run build

FROM nginxinc/nginx-unprivileged:1.25
COPY frontend/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/frontend/dist /usr/share/nginx/html

EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
