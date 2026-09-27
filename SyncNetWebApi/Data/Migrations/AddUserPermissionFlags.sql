START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903230421_AddUserPermissionFlags') THEN
    ALTER TABLE public.dashboard_user ADD allow_add boolean;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903230421_AddUserPermissionFlags') THEN
    ALTER TABLE public.dashboard_user ADD allow_delete boolean;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903230421_AddUserPermissionFlags') THEN
    ALTER TABLE public.dashboard_user ADD allow_edit boolean;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903230421_AddUserPermissionFlags') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260903230421_AddUserPermissionFlags', '10.0.11');
    END IF;
END $EF$;
COMMIT;

