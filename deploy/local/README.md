# Local run with Docker

`docker compose build` can fail on a machine behind a corporate proxy (`NU1301 ... PartialChain` from `dotnet restore` inside
the container). This folder avoids it: the API is published on the machine itself and only packed into the image.

## One-time `.env`

Copy `.env.example` to `.env` and add (the values are yours; avoid `$`, `#` and quotes in passwords):

```
POSTGRES_DB=IRieltor
POSTGRES_USER=irieltor
POSTGRES_PASSWORD=<db-password>
REDIS_PASSWORD=<redis-password>
SERVER_NAME=
HTTP_PORT=8080
```

An empty `SERVER_NAME` means no certificate is requested and Nginx serves plain HTTP. The `JWTConfiguration__Secret`, `Smtp__*` and
`DefaultAdmin__*` values are read from the same file.

## Start / stop

```
.\deploy\local\up.ps1          # publish + docker compose up -d --build
.\deploy\local\up.ps1 -Down    # stop (data stays in the volumes)
```

Then open `http://localhost:8080/health/ready` and `http://localhost:8080/api/slide/public`. Swagger is off outside Development.
Delete the data too: `docker compose -f docker-compose.yml -f docker-compose.local.yml down -v`.

`publish-local/` is generated; do not commit it. Production builds use `WebApi/Dockerfile`.
