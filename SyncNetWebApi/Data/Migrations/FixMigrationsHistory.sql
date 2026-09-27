-- __EFMigrationsHistory currently only lists InitialAuthModule, even though
-- AddPasswordResetFields and AddUserPermissionFlags were already applied (their columns/
-- indexes are confirmed present). This mismatch will make a future `dotnet ef database
-- update` try to RE-apply those two migrations and fail with "column/index already
-- exists". Run this once to bring the history table in line with reality — it does not
-- touch any other data.
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
  ('20260903125321_AddPasswordResetFields', '10.0.11'),
  ('20260903230421_AddUserPermissionFlags', '10.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;
