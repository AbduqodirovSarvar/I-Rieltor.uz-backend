#!/bin/sh
# certbot container (docker-compose.yml): gets one Let's Encrypt certificate for $SERVER_NAME and, when set,
# $API_SERVER_NAME, $ADMIN_SERVER_NAME and $PRESENTATION_SERVER_NAME (stored under the name $SERVER_NAME) through Nginx
# (webroot challenge on port 80) and renews it. Every 12 hours; certbot itself renews only when fewer than
# 30 days are left. Nginx (run-nginx.sh) notices the new files within a minute.
set -u

if [ -z "${SERVER_NAME:-}" ] || [ -z "${LETSENCRYPT_EMAIL:-}" ]; then
    echo "renew: SERVER_NAME or LETSENCRYPT_EMAIL is empty, no certificate is requested (Nginx stays on HTTP)"
    while :; do sleep 86400; done
fi

# LETSENCRYPT_STAGING=1: Let's Encrypt's test CA (not trusted by browsers, but no rate limits) for a first try.
staging=""
[ "${LETSENCRYPT_STAGING:-0}" = "1" ] && staging="--staging"

# Every host name gets a -d; --expand adds a name given later to the existing certificate (restart this container then).
domains="-d $SERVER_NAME"
for name in ${API_SERVER_NAME:-} ${ADMIN_SERVER_NAME:-} ${PRESENTATION_SERVER_NAME:-}; do domains="$domains -d $name"; done

sleep 15   # let Nginx start
while :; do
    # $domains is split into words on purpose.
    # shellcheck disable=SC2086
    certbot certonly --webroot -w /var/www/certbot --cert-name "$SERVER_NAME" --expand $domains \
        --email "$LETSENCRYPT_EMAIL" --agree-tos --no-eff-email --non-interactive --keep-until-expiring $staging \
        || echo "renew: certbot failed; retrying in 12 hours (port 80 must be reachable from the internet and DNS must point here)"
    sleep 43200
done
