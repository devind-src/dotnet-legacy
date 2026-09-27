START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903125321_AddPasswordResetFields') THEN
    ALTER TABLE public.dashboard_user_reset ADD consumed_at timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903125321_AddPasswordResetFields') THEN
    ALTER TABLE public.dashboard_user_reset ADD requested_ip text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903125321_AddPasswordResetFields') THEN
    ALTER TABLE public.dashboard_user_reset ADD user_name text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903125321_AddPasswordResetFields') THEN
    CREATE INDEX "IX_dashboard_user_reset_user_name" ON public.dashboard_user_reset (user_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903125321_AddPasswordResetFields') THEN
    CREATE INDEX "IX_dashboard_user_reset_vcode" ON public.dashboard_user_reset (vcode);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903125321_AddPasswordResetFields') THEN
    CREATE INDEX "IX_dashboard_user_email" ON public.dashboard_user (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903125321_AddPasswordResetFields') THEN
    CREATE UNIQUE INDEX IF NOT EXISTS ux_dashboard_user_email ON public.dashboard_user (lower(email)) WHERE email IS NOT NULL AND email <> '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903125321_AddPasswordResetFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260903125321_AddPasswordResetFields', '10.0.11');
    END IF;
END $EF$;
COMMIT;

