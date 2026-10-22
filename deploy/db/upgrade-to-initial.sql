-- Moves a database created with the OLD migrations (last one: 20261021100451_AuctionRunnerUpAwards) to the single
-- "20261022125136_Initial" migration:
--   1. adds what the old chain still lacked (notifications, audit log, private documents, payment columns);
--   2. replaces the old rows of "__EFMigrationsHistory" with the one Initial row.
-- Run it ONCE, with psql, on an existing database (a new database needs nothing: the app or `dotnet ef database update` creates it).
-- Take a backup first. It stops with an error and changes nothing if the database is not at the expected migration.
--   psql "$CONNECTION" -v ON_ERROR_STOP=1 -f deploy/db/upgrade-to-initial.sql
START TRANSACTION;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261021100451_AuctionRunnerUpAwards') THEN
        RAISE EXCEPTION 'The database is not at migration 20261021100451_AuctionRunnerUpAwards; nothing was changed.';
    END IF;
    IF EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022125136_Initial') THEN
        RAISE EXCEPTION 'The database is already on 20261022125136_Initial; nothing was changed.';
    END IF;
END $$;

ALTER TABLE "Payments" ADD "ProviderCreatedAt" timestamp with time zone;

ALTER TABLE "Payments" ADD "ProviderReason" integer;

CREATE TABLE "AuditLogs" (
    "Id" uuid NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "UserId" uuid,
    "Action" character varying(200) NOT NULL,
    "Method" character varying(10) NOT NULL,
    "Path" character varying(300) NOT NULL,
    "StatusCode" integer NOT NULL,
    "IpAddress" character varying(45),
    "UserAgent" character varying(300),
    "CorrelationId" character varying(64),
    "DurationMs" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" uuid,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Notifications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Channel" character varying(10) NOT NULL,
    "Type" character varying(30) NOT NULL,
    "Status" character varying(10) NOT NULL,
    "Destination" character varying(150) NOT NULL,
    "Subject" character varying(200),
    "Body" character varying(4000) NOT NULL,
    "AuctionId" uuid,
    "Attempts" integer NOT NULL,
    "NextAttemptAt" timestamp with time zone NOT NULL,
    "SentAt" timestamp with time zone,
    "LastError" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" uuid,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Notifications_Attempts" CHECK ("Attempts" >= 0),
    CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "PrivateDocuments" (
    "Id" uuid NOT NULL,
    "OwnerId" uuid NOT NULL,
    "Purpose" character varying(30) NOT NULL,
    "AuctionId" uuid,
    "PaymentId" uuid,
    "OriginalName" character varying(255) NOT NULL,
    "StoredName" character varying(60) NOT NULL,
    "ContentType" character varying(100) NOT NULL,
    "Size" bigint NOT NULL,
    "Description" character varying(500),
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" uuid,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" uuid,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_PrivateDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_PrivateDocuments_Size" CHECK ("Size" > 0),
    CONSTRAINT "FK_PrivateDocuments_Auctions_AuctionId" FOREIGN KEY ("AuctionId") REFERENCES "Auctions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PrivateDocuments_Payments_PaymentId" FOREIGN KEY ("PaymentId") REFERENCES "Payments" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PrivateDocuments_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_AuditLogs_OccurredAt" ON "AuditLogs" ("OccurredAt");

CREATE INDEX "IX_AuditLogs_UserId_OccurredAt" ON "AuditLogs" ("UserId", "OccurredAt");

CREATE INDEX "IX_Notifications_Status_NextAttemptAt" ON "Notifications" ("Status", "NextAttemptAt");

CREATE INDEX "IX_Notifications_UserId_CreatedAt" ON "Notifications" ("UserId", "CreatedAt");

CREATE INDEX "IX_Notifications_UserId_Type_AuctionId" ON "Notifications" ("UserId", "Type", "AuctionId");

CREATE INDEX "IX_PrivateDocuments_AuctionId" ON "PrivateDocuments" ("AuctionId");

CREATE INDEX "IX_PrivateDocuments_OwnerId_CreatedAt" ON "PrivateDocuments" ("OwnerId", "CreatedAt");

CREATE INDEX "IX_PrivateDocuments_PaymentId" ON "PrivateDocuments" ("PaymentId");

CREATE UNIQUE INDEX "IX_PrivateDocuments_StoredName" ON "PrivateDocuments" ("StoredName");

DELETE FROM "__EFMigrationsHistory";
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261022125136_Initial', '10.0.13');

COMMIT;
