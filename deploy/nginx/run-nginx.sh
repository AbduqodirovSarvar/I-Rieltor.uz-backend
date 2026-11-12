#!/bin/sh
# Nginx entrypoint (docker-compose.yml). Serves https.conf when certbot has a certificate for $SERVER_NAME,
# http-only.conf until then. API_SERVER_NAME, ADMIN_SERVER_NAME and PRESENTATION_SERVER_NAME (when set) get their own
# server blocks from vhost-http.conf.template / vhost-https.conf.template, in the same mode (one certificate covers all hosts).
# Checks every minute: a new or renewed certificate is picked up with a reload, no restart and no downtime.
# A certificate Nginx cannot use (nginx -t fails, e.g. key and certificate do not match) never takes the site down:
# Nginx stays on HTTP and tries again every minute.
set -eu

LIVE="/etc/letsencrypt/live/${SERVER_NAME:-none}"
SITE=/etc/nginx/site
mkdir -p "$SITE"

MODE=http

# The site's own block plus one block per extra host that has a name. $1: http or https.
write_site() {
    if [ "$1" = https ]; then cp /opt/nginx/https.conf "$SITE/active.conf"; else cp /opt/nginx/http-only.conf "$SITE/active.conf"; fi
    for pair in "${API_SERVER_NAME:-}:api-host-locations.conf" "${ADMIN_SERVER_NAME:-}:admin-locations.conf" \
        "${PRESENTATION_SERVER_NAME:-}:presentation-locations.conf"; do
        name=${pair%%:*}
        [ -n "$name" ] || continue
        VHOST_NAME=$name VHOST_LOCATIONS=${pair#*:} envsubst '${VHOST_NAME} ${VHOST_LOCATIONS}' \
            < "/opt/nginx/vhost-$1.conf.template" >> "$SITE/active.conf"
    done
}

select_site() {
    if [ -n "${SERVER_NAME:-}" ] && [ -s "$LIVE/fullchain.pem" ] && [ -s "$LIVE/privkey.pem" ]; then
        ln -sfn "$LIVE" "$SITE/cert"
        write_site https
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
    write_site http
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
