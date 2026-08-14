#!/bin/sh
set -e

envsubst '${LETSENCRYPT_EMAIL}' \
  < /etc/traefik/traefik.yml.template \
  > /etc/traefik/traefik.yml

if [ -n "${LETSENCRYPT_EMAIL}" ]; then
  export TLS_BLOCK="{ certResolver: letsencrypt }"
else
  # Local / empty email: Traefik default (self-signed) certificate.
  export TLS_BLOCK="{}"
fi

envsubst '${FRONTEND_HOST} ${BACKEND_HOST} ${FRONTEND_UPSTREAM} ${BACKEND_UPSTREAM} ${TLS_BLOCK}' \
  < /etc/traefik/dynamic/routes.yml.template \
  > /etc/traefik/dynamic/routes.yml

exec traefik --configFile=/etc/traefik/traefik.yml
