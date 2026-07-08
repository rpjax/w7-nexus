FROM traefik:v3.3
RUN apk add --no-cache gettext
COPY traefik.yml.template /etc/traefik/traefik.yml.template
COPY dynamic/ /etc/traefik/dynamic/
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh
ENTRYPOINT ["/docker-entrypoint.sh"]
