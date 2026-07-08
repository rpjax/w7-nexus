ARG ENV=prod

FROM node:22-alpine AS build
ARG ENV=prod
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci
COPY . .

# Map CLI env → Vite --mode
RUN case "$ENV" in \
      dev)  MODE=development ;; \
      prod) MODE=production ;; \
      *)    echo "Unsupported ENV=$ENV" >&2; exit 1 ;; \
    esac \
    && echo "$MODE" > /tmp/vite-mode \
    && npm run build -- --mode "$MODE"

FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
