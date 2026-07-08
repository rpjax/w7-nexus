#!/bin/sh
set -e

envsubst '${LETSENCRYPT_EMAIL}' \
  < /etc/traefik/traefik.yml.template \
  > /etc/traefik/traefik.yml

envsubst '${FRONTEND_HOST} ${BACKEND_HOST} ${FRONTEND_UPSTREAM} ${BACKEND_UPSTREAM}' \
  < /etc/traefik/dynamic/routes.yml.template \
  > /etc/traefik/dynamic/routes.yml

exec traefik --configFile=/etc/traefik/traefik.yml
