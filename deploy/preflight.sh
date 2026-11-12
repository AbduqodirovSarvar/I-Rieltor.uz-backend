#!/bin/sh
# Checks the server's .env before the first start (and after every change to it):  sh I-Rieltor.uz-backend/deploy/preflight.sh
# Prints what is missing or unsafe; exit code 1 when something must be fixed first. Never prints a secret's value.
set -u

ENV_FILE="${1:-$(dirname "$0")/../.env}"
[ -f "$ENV_FILE" ] || { echo "preflight: $ENV_FILE not found (copy .env.example to .env and fill it in)"; exit 1; }

errors=0
warnings=0
fail() { echo "  ERROR   $1"; errors=$((errors + 1)); }
warn() { echo "  WARNING $1"; warnings=$((warnings + 1)); }

# The value of a key (the last line wins, as in Docker), without surrounding quotes.
get() { grep -E "^[[:space:]]*$1=" "$ENV_FILE" | tail -n 1 | cut -d= -f2- | sed -e 's/^"//' -e 's/"$//'; }

echo "preflight: $ENV_FILE"

for key in POSTGRES_PASSWORD REDIS_PASSWORD JWTConfiguration__Secret DefaultAdmin__Email DefaultAdmin__Password \
    SERVER_NAME LETSENCRYPT_EMAIL AllowedHosts FileStorage__PublicBaseUrl EmailConfirmation__LinkBaseUrl \
    Smtp__UserName Smtp__Password Smtp__FromAddress Eimzo__BaseUrl Eimzo__SiteHost; do
    value=$(get "$key")
    if [ -z "$value" ]; then
        fail "$key is empty"
    elif printf '%s' "$value" | grep -q '[<>]'; then
        fail "$key still holds a <placeholder> from .env.example"
    fi
done

secret=$(get JWTConfiguration__Secret)
[ "${#secret}" -ge 32 ] || fail "JWTConfiguration__Secret is shorter than 32 characters (openssl rand -base64 64)"

[ "$(get DefaultAdmin__Email)" = "admin@example.com" ] && fail "DefaultAdmin__Email is still the example address"

server=$(get SERVER_NAME)
admin=$(get ADMIN_SERVER_NAME)
apihost=$(get API_SERVER_NAME)
presentation=$(get PRESENTATION_SERVER_NAME)
hosts=";$(get AllowedHosts);"
for name in "$server" "$apihost" "$admin" api; do
    [ -n "$name" ] || continue
    case "$hosts" in *";$name;"*) ;; *) fail "AllowedHosts does not contain \"$name\"" ;; esac
done
[ -n "$admin" ] || warn "ADMIN_SERVER_NAME is empty: the admin panel is not served (only http://127.0.0.1:ADMIN_PORT on the server)"

case "$(get FileStorage__PublicBaseUrl)" in "https://$server"|"https://$server/") ;; *) warn "FileStorage__PublicBaseUrl is not https://$server" ;; esac
[ "$(get Swagger__Enabled)" = "false" ] || warn "Swagger__Enabled is not false: the API description is public at /swagger"
[ "$(get HTTP_PORT)" = "80" ] || warn "HTTP_PORT is not 80: Let's Encrypt checks the domain on port 80"
[ "$(get ADMIN_BIND)" = "0.0.0.0" ] && fail "ADMIN_BIND=0.0.0.0 serves the admin panel over plain HTTP to everybody"
[ "$(get RateLimiting__AuthPermitPerMinute)" != "" ] && warn "RateLimiting__AuthPermitPerMinute is set (a test value?)"
[ "$(get Eimzo__SiteHost)" = "localhost" ] && fail "Eimzo__SiteHost is localhost (the E-IMZO stand-in's setting)"

# Every name must already point at this server, or certbot cannot get the certificate.
if command -v getent >/dev/null 2>&1; then
    for name in "$server" "$apihost" "$admin" "$presentation"; do
        [ -n "$name" ] || continue
        getent hosts "$name" >/dev/null 2>&1 || warn "DNS: $name does not resolve yet"
    done
fi

# The stand-in E-IMZO server must never run on a real site.
root=$(dirname "$0")/../..
if docker compose -f "$root/docker-compose.yml" ps --services 2>/dev/null | grep -qx e-imzo-server; then
    fail "the E-IMZO stand-in (e-imzo-server) is running: start with docker-compose.yml only, not docker-compose.local.yml"
fi

echo "preflight: $errors error(s), $warnings warning(s)"
[ "$errors" -eq 0 ]
