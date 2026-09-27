using Microsoft.EntityFrameworkCore;
using SyncNetApi.Entities;

namespace SyncNetApi.Data
{
    /// <summary>
    /// Phase 1 scope: user management + auth only (mirrors the "User", "User Logs", "Role"
    /// and "Menu" regions of the legacy DbSwitchNetService, plus new tables for JWT support).
    ///
    /// When the next module is migrated (e.g. Merchant, Terminal), add its DbSet here and
    /// copy the matching entity class from the old Models/DbSwitchNet folder into Entities/ —
    /// they were already written against PostgreSQL so no type changes should be needed.
    /// </summary>
    public class SyncNetDbContext : DbContext
    {
        public SyncNetDbContext(DbContextOptions<SyncNetDbContext> options) : base(options) { }

        // Auth / user management
        public DbSet<DashboardUsers> Users => Set<DashboardUsers>();
        public DbSet<DashboardUserLog> UserLog => Set<DashboardUserLog>();
        public DbSet<DashboardUserReset> UserReset => Set<DashboardUserReset>();
        public DbSet<DashboardRoles> Roles => Set<DashboardRoles>();
        public DbSet<DashboardMenus> Menus => Set<DashboardMenus>();
        public DbSet<DashboardRoleMenu> RoleMenu => Set<DashboardRoleMenu>();

        // Audit trail (shared by every future module)
        public DbSet<SwAudit> Audits => Set<SwAudit>();

        // Phase 4: PosBase — Participant / Merchant / SubMerchant / Store / Terminal chain
        public DbSet<SwParticipant> Participants => Set<SwParticipant>();
        public DbSet<SwMerchant> Merchants => Set<SwMerchant>();
        public DbSet<SwSubMerchant> SubMerchants => Set<SwSubMerchant>();
        public DbSet<SwStore> Stores => Set<SwStore>();
        public DbSet<SwTerminal> Terminals => Set<SwTerminal>();

        // Phase 4: PosBase > Base Config
        public DbSet<SwMerchantGroup> MerchantGroups => Set<SwMerchantGroup>();
        public DbSet<SwSubMerchantGroup> SubMerchantGroups => Set<SwSubMerchantGroup>();
        public DbSet<SwPosnetBin> PosnetBins => Set<SwPosnetBin>();
        public DbSet<SwTerminalLimit> TerminalLimits => Set<SwTerminalLimit>();
        public DbSet<SwTerminalImage> TerminalImages => Set<SwTerminalImage>();
        public DbSet<SwTerminalClient> TerminalClients => Set<SwTerminalClient>();
        public DbSet<SoundBoxTerminal> SoundBoxTerminals => Set<SoundBoxTerminal>();

        // Terminal > Key Management / Device tab linked 1:1 rows
        public DbSet<SwTerminalKey> TerminalKeys => Set<SwTerminalKey>();
        public DbSet<SwTerminalInit> TerminalInits => Set<SwTerminalInit>();

        // Merchant > Key Management tab linked 1:1 row
        public DbSet<SwMerchantKey> MerchantKeys => Set<SwMerchantKey>();

        // Read-only lookups from other (not-yet-built) modules, needed only for dropdowns
        public DbSet<SwCity> Cities => Set<SwCity>();
        public DbSet<SwBank> Banks => Set<SwBank>();
        public DbSet<SwBrand> Brands => Set<SwBrand>();
        public DbSet<SwProvince> Provinces => Set<SwProvince>();
        public DbSet<SwProductBin> ProductBins => Set<SwProductBin>();
        public DbSet<SwSupportTeam> SupportTeams => Set<SwSupportTeam>();
        public DbSet<SwSupportMember> SupportMembers => Set<SwSupportMember>();
        public DbSet<SwBusinessDate> BusinessDates => Set<SwBusinessDate>();
        public DbSet<SwPublicHoliday> PublicHolidays => Set<SwPublicHoliday>();
        public DbSet<SwJob> Jobs => Set<SwJob>();
        public DbSet<SwJobLog> JobLogs => Set<SwJobLog>();
        public DbSet<SwCleaner> Cleaners => Set<SwCleaner>();
        public DbSet<SwProduct> Products => Set<SwProduct>();
        public DbSet<SwJobFee> JobFees => Set<SwJobFee>();
        public DbSet<SwJobFeeDetail> JobFeeDetails => Set<SwJobFeeDetail>();
        public DbSet<SwApp> Apps => Set<SwApp>();
        public DbSet<SwNodes> Nodes => Set<SwNodes>();
        public DbSet<SwCryptoKey> CryptoKeys => Set<SwCryptoKey>();
        public DbSet<SwConnection> Connections => Set<SwConnection>();
        public DbSet<VaAccount> VaAccounts => Set<VaAccount>();
        public DbSet<SwMerchantCriteria> MerchantCriteria => Set<SwMerchantCriteria>();
        public DbSet<SwMcc> MccCodes => Set<SwMcc>();

        // Phase 5 — Configuration > Base batch 1
        public DbSet<SwAccountType> AccountTypes => Set<SwAccountType>();
        public DbSet<SwCountry> Countries => Set<SwCountry>();
        public DbSet<SwCurrency> Currencies => Set<SwCurrency>();
        public DbSet<SwTranName> TranNames => Set<SwTranName>();

        // Phase R — Routing (Source / BIN / Product / Dynamic)
        public DbSet<SwRoutesBySource> RoutesBySource => Set<SwRoutesBySource>();
        public DbSet<SwRoutesByBin> RoutesByBin => Set<SwRoutesByBin>();
        public DbSet<SwRoutesByInst> RoutesByInst => Set<SwRoutesByInst>();
        public DbSet<SwRoutesMargin> RoutesMargin => Set<SwRoutesMargin>();
        public DbSet<SwRoutesByInstAlt> RoutesByInstAlt => Set<SwRoutesByInstAlt>();

        // Phase R2 — Routing > Margin failover (Config / Supplier Status / Log)
        public DbSet<SwRoutesSupplierStatus> RoutesSupplierStatus => Set<SwRoutesSupplierStatus>();
        public DbSet<SwRoutesFailoverConfig> RoutesFailoverConfig => Set<SwRoutesFailoverConfig>();
        public DbSet<SwRoutesFailoverLog> RoutesFailoverLog => Set<SwRoutesFailoverLog>();
        public DbSet<SwRoutesSchedule> RoutesSchedule => Set<SwRoutesSchedule>();
        public DbSet<SwRoutesScheduleHist> RoutesScheduleHist => Set<SwRoutesScheduleHist>();

        // Phase P1 — Product > Category / Mapping / Transfer
        public DbSet<SwProductCategory> ProductCategories => Set<SwProductCategory>();
        public DbSet<SwProductMapping> ProductMappings => Set<SwProductMapping>();
        public DbSet<SwProductTransfer> ProductTransfers => Set<SwProductTransfer>();

        // Phase P2 — Product > Merchant
        public DbSet<SwMerchantProduct> MerchantProducts => Set<SwMerchantProduct>();

        // Phase P3 — Product > Prices (Supplier / Merchant)
        public DbSet<SwMarginSupplier> MarginSuppliers => Set<SwMarginSupplier>();
        public DbSet<SwMarginMerchant> MarginMerchants => Set<SwMarginMerchant>();

        // Phase P5 — Product > Fees (Product Fees / Additional Fees)
        public DbSet<SwFees> Fees => Set<SwFees>();
        public DbSet<SwFeeAdditional> FeeAdditionals => Set<SwFeeAdditional>();

        // Phase P6 — Product > Fees (Product Promo / Tiering Fees)
        public DbSet<SwFeesPromo> FeesPromos => Set<SwFeesPromo>();
        public DbSet<SwFeesTiering> FeesTierings => Set<SwFeesTiering>();

        // Phase Card-1 — Card > Base (Group upgraded from read-only lookup, BIN/Account/Hotcard new)
        public DbSet<SwGroup> CardGroups => Set<SwGroup>();
        public DbSet<SwBin> CardBins => Set<SwBin>();
        public DbSet<SwAccount> CardAccounts => Set<SwAccount>();
        public DbSet<SwHotcard> CardHotcards => Set<SwHotcard>();

        // Phase Card-2 — Card > Profile > Issuer (master + Contact/Key 1:1 children + Branch 1:N)
        public DbSet<CmsIssuer> CardIssuers => Set<CmsIssuer>();
        public DbSet<CmsIssuerContact> CardIssuerContacts => Set<CmsIssuerContact>();
        public DbSet<CmsIssuerKey> CardIssuerKeys => Set<CmsIssuerKey>();
        public DbSet<CmsBranch> CardBranches => Set<CmsBranch>();

        // Phase Card-2 — Card > Profile > Product (+ Limit per Channel child, ON DELETE CASCADE)
        public DbSet<CmsProduct> CardProducts => Set<CmsProduct>();
        public DbSet<CmsProductLimit> CardProductLimits => Set<CmsProductLimit>();

        // Phase Card-2 — Card > Profile > Override Limit (standalone, no real FK)
        public DbSet<CmsProductLimitOverride> CardOverrideLimits => Set<CmsProductLimitOverride>();

        // Phase VA-1 — Virtual Account > Group (VaAccount upgraded from read-only lookup above)
        public DbSet<VaGroup> VaGroups => Set<VaGroup>();

        // Phase VA-2/VA-3 — Virtual Account > Topup / Adjustment / Approval
        public DbSet<VaTransRequest> VaTransRequests => Set<VaTransRequest>();

        // Phase VA-3 — core switch ledger table, written only by the Approval workflow here.
        // Shared with the live switching engine — see VaTransRequestService.ApproveAsync.
        public DbSet<SwTransPg> SwTransactions => Set<SwTransPg>();

        // Phase Cashout-1 — Cashout Account > Withdrawal / Fee Mini ATM
        public DbSet<SwTerminalBank> CashoutWithdrawals => Set<SwTerminalBank>();
        public DbSet<SwCashoutBank> CashoutMiniAtms => Set<SwCashoutBank>();

        // Phase Cashout-2 — Cashout Account > QRIS Static (CRUD only, Import/Export CSV out of scope)
        public DbSet<SwAgentBank> AgentBanks => Set<SwAgentBank>();

        // Phase HSM — Device / Service (Console is stateless, no table of its own)
        public DbSet<SwCryptoHsm> HsmDevices => Set<SwCryptoHsm>();
        public DbSet<SwCryptoService> HsmServices => Set<SwCryptoService>();

        // API-specific: JWT lifecycle
        public DbSet<BlacklistedToken> BlacklistedTokens => Set<BlacklistedToken>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(x => x.TokenHash);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(x => x.UserName);

            modelBuilder.Entity<BlacklistedToken>()
                .HasIndex(x => x.ExpiresAt);

            // Plain (non-unique, case-sensitive) index for ordinary lookups — the real
            // uniqueness guarantee is a case-insensitive partial unique index created via
            // raw SQL in the migration (see Data/Migrations), since EF Core's cross-provider
            // model has no clean way to express `lower(email)` as an index key.
            modelBuilder.Entity<DashboardUsers>()
                .HasIndex(x => x.email);

            modelBuilder.Entity<DashboardUserReset>()
                .HasIndex(x => x.vcode);

            modelBuilder.Entity<DashboardUserReset>()
                .HasIndex(x => x.user_name);
        }

        #region Audit helpers (ported from legacy DbSwitchNetContext, bug fixed)
        // NOTE: the original implementation had the ternary backwards, so empty
        // old/new values ended up being run through ToJSON() while populated ones
        // were stored raw. Fixed here: empty stays empty, populated values are
        // stored as-is (caller is expected to pass an already-serialized JSON string).
        //
        // sw_audit.oldvalue/newvalue are real `json` columns in Postgres — an empty string
        // is not valid JSON and Postgres rejects the insert (22P02). The JSON literal "null"
        // is what "no data" for this side of the change actually means, and is valid JSON.
        private const string NoAuditData = "null";

        public void AddAuditInsert(string newData, string tableName, string entryBy)
            => AddAudit(NoAuditData, newData, tableName, "I", entryBy);

        public void AddAuditUpdate(string oldData, string newData, string tableName, string entryBy)
            => AddAudit(oldData, newData, tableName, "U", entryBy);

        public void AddAuditDelete(string oldData, string tableName, string entryBy)
            => AddAudit(oldData, NoAuditData, tableName, "D", entryBy);

        private void AddAudit(string oldData, string newData, string tableName, string crudType, string entryBy)
        {
            Audits.Add(new SwAudit
            {
                oldvalue = oldData,
                newvalue = newData,
                tablename = tableName,
                type = crudType,
                // updatedate is "timestamp without time zone" (explicitly annotated, §7.27) —
                // Npgsql rejects writing a Kind=Utc DateTime to that column type outright, so
                // the same UTC instant is re-tagged Unspecified here (not switched to
                // DateTime.Now — that would silently change stored values to local time and
                // break consistency with every row written before this fix). Superseded the
                // previous "must be UTC, Npgsql rejects Local" comment, which was true only
                // while this column was unannotated (implicitly timestamptz).
                updatedate = System.DateTime.SpecifyKind(System.DateTime.UtcNow, System.DateTimeKind.Unspecified),
                username = entryBy
            });
        }
        #endregion
    }
}
