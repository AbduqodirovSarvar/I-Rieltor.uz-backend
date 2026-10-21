#!/bin/sh
# Nginx entrypoint (docker-compose.yml). Serves https.conf when certbot has a certificate for $SERVER_NAME,
# http-only.conf until then, and checks every minute: a new or renewed certificate is picked up with a reload,
# no restart and no downtime. A certificate Nginx cannot use (nginx -t fails, e.g. key and certificate do not
# match) never takes the site down: Nginx stays on HTTP and tries again every minute.
set -eu

LIVE="/etc/letsencrypt/live/${SERVER_NAME:-none}"
SITE=/etc/nginx/site
mkdir -p "$SITE"

MODE=http

select_site() {
    if [ -n "${SERVER_NAME:-}" ] && [ -s "$LIVE/fullchain.pem" ] && [ -s "$LIVE/privkey.pem" ]; then
        ln -sfn "$LIVE" "$SITE/cert"
        cp /opt/nginx/https.conf "$SITE/active.conf"
        if nginx -t -q 2>/dev/null; then
            MODE=https
            echo "run-nginx: HTTPS with the certificate in $LIVE"
            return
        fi
        echo "run-nginx: the certificate in $LIVE cannot be used (nginx -t failed), staying on HTTP" >&2
        MODE=broken
    else
        MODE=http
        echo "run-nginx: no certificate in $LIVE yet, HTTP only"
    fi
    cp /opt/nginx/http-only.conf "$SITE/active.conf"
}

fingerprint() {
    cat "$LIVE/fullchain.pem" 2>/dev/null | md5sum
}

select_site

(
    last=$(fingerprint)
    while sleep 60; do
        now=$(fingerprint)
        # broken: certbot may have been caught in the middle of replacing the files; look again.
        if [ "$now" != "$last" ] || [ "$MODE" = broken ]; then
            last=$now
            select_site
            nginx -t && nginx -s reload
        fi
    done
) &

exec /docker-entrypoint.sh nginx -g 'daemon off;'
