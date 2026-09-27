START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260904030953_FixSwAuditIdType') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260904030953_FixSwAuditIdType', '10.0.11');
    END IF;
END $EF$;
COMMIT;

