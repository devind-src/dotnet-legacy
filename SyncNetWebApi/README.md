# SyncNetApi — Phase 1 Skeleton (Auth + User Management)

Standalone ASP.NET Core Web API (.NET 10) extracted from the legacy `SyncNetWeb` Blazor
Server app. This phase covers **only** the user/auth module, per the agreed priority:
login, logout, refresh, change password, and admin user CRUD. Every other module
(Merchant, Terminal, VA, Routing, etc.) will be migrated in later phases following the
same pattern established here.

## What's implemented

- **JWT authentication** with short-lived access tokens (default 15 min) + rotating
  refresh tokens (default 7 days, single-use, reuse-detection revokes the whole family).
- **Logout blacklist**: revoked access tokens are rejected immediately via
  `JwtBearerEvents.OnTokenValidated`, even though they haven't naturally expired yet.
  Persisted in Postgres (`api_token_blacklist`) with an in-memory cache fast path.
- **Password hashing** via ASP.NET Core Identity's built-in `PasswordHasher<T>`
  (PBKDF2-HMAC-SHA256). Existing accounts created by the legacy app (DES-encrypted
  password) are transparently verified and **auto-upgraded** to the new hash on their
  next successful login — no forced password reset needed. See
  `Services/Auth/ILegacyPasswordVerifier.cs` — the actual DES verification logic still
  needs to be ported from the old app's `Library/NbCreden.cs` (see TODO below).
- **License check runs once at startup**, not per-request (see `Program.cs`, before
  `app.Run()`). Ported from the legacy app's `Common/LicenseManager.cs` (see
  `Services/License/LicenseKeyCodec.cs`) — validates that `License:LicenseKey` decrypts to
  this machine's disk-serial hash plus a still-future expiry date. There was no key-issuing
  tool in the legacy codebase (only the verifier), so one was added at
  `Tools/LicenseKeyGenerator` — run `dotnet run` in that folder to print this machine's
  serial number and a matching key (defaults to a 10-year expiry; `--days N` / `--expiry
  yyyyMMdd` / `--serial HEX` to override).
- **Rate limiting** on `/api/v1/auth/login` (10 attempts/min per IP) on top of the
  existing per-account retry lock.
- **Audit trail** ported from `DbSwitchNetContext`, with a bug fixed: the original
  `AddAudit` had its empty/populated ternary backwards.
- **OpenAPI + Scalar** interactive docs at `/scalar/v1` in Development, with the Bearer
  scheme pre-wired so you can authorize and call protected endpoints from the browser.
- **CORS** is wide open to configured origins only — no assumption is made about which
  UI will consume this API (MVC, React, anything with a `fetch`/`HttpClient`).

## Endpoints (phase 1)

| Method | Route | Auth | Notes |
|---|---|---|---|
| POST | `/api/v1/auth/login` | — | rate-limited; returns a NEW_PASSWORD_REQUIRED challenge instead of tokens if must_change_password is set |
| POST | `/api/v1/auth/login/new-password` | — | completes the NEW_PASSWORD_REQUIRED challenge |
| POST | `/api/v1/auth/refresh` | — | rotates the refresh token |
| POST | `/api/v1/auth/logout` | Bearer | blacklists current access token, optionally revokes a specific refresh token |
| POST | `/api/v1/auth/change-password` | Bearer | requires old password; revokes every other session |
| POST | `/api/v1/auth/forgot-password` | — | rate-limited; always the same generic response |
| POST | `/api/v1/auth/reset-password` | — | rate-limited; consumes a single-use token from forgot-password |
| GET | `/api/v1/dev/last-reset-link` | — | **Development only** — retrieves SimulatedEmailSender's last link, for testing forgot-password without a real mailbox |
| GET | `/api/v1/users` | Bearer, role `admin` | `?filter=` optional |
| GET | `/api/v1/users/{userName}` | Bearer, role `admin` | |
| POST | `/api/v1/users` | Bearer, role `admin` + caller's `allow_add` | |
| PUT | `/api/v1/users/{userName}` | Bearer, role `admin` + caller's `allow_edit` | |
| DELETE | `/api/v1/users/{userName}` | Bearer, role `admin` + caller's `allow_delete` | |
| POST | `/api/v1/users/{userName}/reset-password` | Bearer, role `admin` + caller's `allow_edit` | admin-initiated, no old password needed, forces must_change_password on the target |

> **Role check is case-sensitive.** `dashboard_role.role_name` values in this DB are lowercase
> (`admin`, `spv`, `ops`, `mon`), so `UsersController` checks `role = "admin"`, not `"Admin"` —
> match whatever case your own `dashboard_role` rows actually use if you seed different ones.
>
> **Per-user add/edit/delete rights**: `dashboard_user.allow_add/allow_edit/allow_delete`
> (nullable) override `dashboard_role.flag_add/flag_edit/flag_delete` when set; `null` means
> "inherit whatever the role currently grants" (re-evaluated live on every request, not a
> one-time copy). See `IUserService.GetEffectivePermissionsAsync`.

## Getting started

```bash
cd SyncNetApi

# secrets (never commit real values — see appsettings.Development.json for the exact commands)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DbConnection" "Host=localhost;Port=5432;Database=syncnet;Username=app;Password=Password1!"
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"

# license (see "License check" above) — get this machine's key from the generator tool:
#   cd ../Tools/LicenseKeyGenerator && dotnet run
dotnet user-secrets set "License:LicenseKey" "<paste the generated key>"

# legacy DES password verification (optional — only needed to let accounts created by
# the old Blazor app log in). Point at wherever Resources.bin + PrivateKey.pem were
# handed to you; a Credentials/ folder next to this repo is gitignored for exactly this.
dotnet user-secrets set "LegacyCredentials:ResourcesPath" "<path>/Resources.bin"
dotnet user-secrets set "LegacyCredentials:PrivateKeyPath" "<path>/PrivateKey.pem"

# apply the migration that creates this API's two new tables (api_token_blacklist,
# api_refresh_token). Everything else (dashboard_user, dashboard_role, ...) already
# exists in the production syncnet database and is NOT touched by this migration
# (Data/Migrations/InitialAuthModule.cs was hand-trimmed for exactly that reason) —
# review Data/Migrations/InitialAuthModule.sql if you'd rather apply it manually.
dotnet ef database update

dotnet run
```

The `dotnet ef` commands need the `dotnet-ef` local tool once per machine:
`dotnet tool restore` from the repo root (`dotnet-tools.json` pins the version to match
`Microsoft.EntityFrameworkCore.Design`).

Then open `https://localhost:<port>/scalar/v1` to try it out.

> The `dashboard_role` table needs at least one row with `role_name = 'Admin'` for the
> `UsersController` authorization policy to let anyone in — seed that (and a first user)
> manually if your `syncnet` database doesn't already have one from the Blazor app.

## Known gaps to close before production

1. ~~Port the legacy DES password verifier~~ — done, once `Credentials/Resources.bin` +
   `Credentials/PrivateKey.pem` were provided. `Services/Auth/LegacyResourcesLoader.cs`
   ports the hybrid RSA-OAEP/AES-GCM loader from `Common/Resources.cs` (loads once at
   startup, non-fatal if the files are missing/misconfigured — unlike the license check,
   this must not block the app for accounts that don't need legacy login).
   `Services/Auth/LegacyPasswordVerifier.cs` ports `Library/NbCreden.cs` +
   `Cryptography/DesAlgorithm.cs`: it RE-ENCRYPTS `"{userName}|{password}"` and compares
   ciphertext against `dashboard_user.password`, exactly like the legacy
   `AccountController.Login` did (it never decrypts the stored value). Configure via
   `LegacyCredentials:ResourcesPath` / `LegacyCredentials:PrivateKeyPath` (user-secrets or
   env vars — see Getting started below); keep the `Credentials/` folder itself out of
   version control (already in `.gitignore` at the repo root).
2. ~~Port the real license check~~ — done. `Services/License/LicenseKeyCodec.cs` +
   `LicenseService.cs` port `Common/LicenseManager.cs` + `Common/ServerInfo.cs` faithfully
   (no external key material needed, unlike the DES verifier above — the wrapping AES
   key/IV is a fixed constant in the legacy generator). `Tools/LicenseKeyGenerator` fills
   in for the legacy key-issuing tool, which wasn't part of this codebase.
3. **Captcha / bot mitigation** on login — `LoginRequest.CaptchaToken` is a placeholder
   field; wire it to whatever challenge provider you choose (rate limiting alone covers
   basic brute force, but not credential-stuffing bots).
4. Decide on the connection-string encryption story. The old app protects it with a
   hybrid RSA+AES-GCM scheme keyed from an external `Resources.bin` — this skeleton
   uses a plain connection string via user-secrets/environment variables instead, which
   is fine for typical deployments (secret manager, Key Vault, env vars) but port the
   old scheme over if you specifically need it.

## Adding the next module

Each future module (Merchant, Terminal, Product, VA, ...) should follow the same shape:

1. Copy the entity class(es) from the old `Models/DbSwitchNet/*.cs` into `Entities/`
   (they're already written for PostgreSQL — no changes should be needed).
2. Add the matching `DbSet<T>` to `SyncNetDbContext`.
3. Create `Dtos/<Module>/*.cs` — request/response shapes, never expose entities directly
   (this matters most for anything touching `sw_crypto_keys`, `sw_merchant_key`,
   `sw_term_key` — those fields should never round-trip to a client as-is).
4. Create `Services/<Module>/I<Module>Service.cs` + implementation, porting the relevant
   region from the old `DbSwitchNetService.cs` — the CRUD pattern there
   (Create/Update/Delete/GetRow/GetRecords) maps close to 1:1 onto this layer.
5. Create `Controllers/<Module>Controller.cs` with REST verbs and proper HTTP status
   codes (see `UsersController` for the pattern: `NotFoundException` → 404,
   `ConflictException` → 409, etc. — see `Common/ServiceExceptions.cs`).
6. Raw SQL: if a legacy method used `DbSwitchNetSql.Execute(string query)` with string
   interpolation, don't port it as-is — either rewrite as EF Core LINQ, or use the
   parameterized Dapper overloads (`QueryAsync<T>(sql, param)`) that already exist in
   the old `DbSwitchNetSql.cs`.
