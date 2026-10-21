#!/bin/sh
# certbot container (docker-compose.yml): gets the Let's Encrypt certificate for $SERVER_NAME through Nginx
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

sleep 15   # let Nginx start
while :; do
    certbot certonly --webroot -w /var/www/certbot -d "$SERVER_NAME" \
        --email "$LETSENCRYPT_EMAIL" --agree-tos --no-eff-email --non-interactive --keep-until-expiring $staging \
        || echo "renew: certbot failed; retrying in 12 hours (port 80 must be reachable from the internet and DNS must point here)"
    sleep 43200
done
