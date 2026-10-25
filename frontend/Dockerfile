# Builds the UI and serves it with nginx, which also forwards /api to the Quotes API (same origin, so no CORS).
# docker build -t advisor-quotes-web frontend
# docker run -p 3000:80 -e QUOTES_API_URL=http://host.docker.internal:8080 advisor-quotes-web

FROM node:24.14.0-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.29.3-alpine AS runtime
ENV QUOTES_API_URL=http://api:8080
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
