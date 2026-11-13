# Build context is the repository root.
FROM node:22-alpine AS build
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

FROM nginx:1.27-alpine AS final
ENV GATEWAY_UPSTREAM=http://gateway:8080 \
    NGINX_ENVSUBST_FILTER=GATEWAY_UPSTREAM
COPY frontend/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
