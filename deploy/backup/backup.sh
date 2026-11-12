#!/bin/sh
# backup container (docker-compose.yml, T-33): every day at BACKUP_AT_UTC (default 22:00 = 03:00 in Tashkent) and
# once at start, a pg_dump of the database and a tar.gz of the uploaded files go to /backups (a host folder);
# copies older than BACKUP_KEEP_DAYS (default 14) are deleted. "backup.sh now" makes one backup and exits.
# /backups/last-success is touched after a complete backup; the container healthcheck watches it.
set -u
umask 077   # the dumps hold personal data

AT="${BACKUP_AT_UTC:-22:00}"
KEEP="${BACKUP_KEEP_DAYS:-14}"
mkdir -p /backups/db /backups/files

backup() {
    stamp=$(date -u +%Y%m%d-%H%M%S)
    ok=1

    db="/backups/db/irieltor-$stamp.dump"
    # Custom format: compressed, restorable table by table; read back once to be sure it is complete.
    if pg_dump -h postgres -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f "$db.partial" \
        && pg_restore --list "$db.partial" > /dev/null; then
        mv "$db.partial" "$db"
        echo "backup: database -> $db ($(du -h "$db" | cut -f1))"
    else
        rm -f "$db.partial"; ok=0
        echo "backup: DATABASE BACKUP FAILED" >&2
    fi

    files="/backups/files/files-$stamp.tar.gz"
    if tar -czf "$files.partial" -C /data/files . && gzip -t "$files.partial"; then
        mv "$files.partial" "$files"
        echo "backup: files -> $files ($(du -h "$files" | cut -f1))"
    else
        rm -f "$files.partial"; ok=0
        echo "backup: FILES BACKUP FAILED" >&2
    fi

    find /backups/db /backups/files -type f -mtime +"$KEEP" -print -delete | sed 's/^/backup: removed old /'
    [ "$ok" = 1 ] && touch /backups/last-success
}

if [ "${1:-}" = "now" ]; then
    backup
    exit 0
fi

[ "${BACKUP_ON_START:-1}" = "1" ] && backup
last=$(date -u +%Y%m%d)
while sleep 30; do
    today=$(date -u +%Y%m%d)
    if [ "$(date -u +%H:%M)" = "$AT" ] && [ "$last" != "$today" ]; then
        last=$today
        backup
    fi
done
