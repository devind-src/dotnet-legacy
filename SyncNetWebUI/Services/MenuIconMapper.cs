using MudBlazor;

namespace SyncNetWasm.Services
{
    /// <summary>
    /// The legacy menu table stores Open Iconic CSS classes ("oi oi-cog"), not MudBlazor SVG
    /// icon constants — mapped to the closest Material equivalent so menus stay visually
    /// consistent with the rest of the (MudBlazor/Material) UI. Shared by NavMenu and Home
    /// (quick-access cards) so the mapping never drifts between the two. Extend as new oi-*
    /// classes show up in menu rows this doesn't recognize yet.
    /// </summary>
    public static class MenuIconMapper
    {
        public static string IconFor(string ociClass) => ociClass switch
        {
            "oi oi-cog" => Icons.Material.Filled.Settings,
            "oi oi-calendar" => Icons.Material.Filled.CalendarMonth,
            "oi oi-list" => Icons.Material.Filled.List,
            "oi oi-credit-card" => Icons.Material.Filled.CreditCard,
            "oi oi-key" => Icons.Material.Filled.VpnKey,
            "oi oi-fork" => Icons.Material.Filled.CallSplit,
            "oi oi-briefcase" => Icons.Material.Filled.Work,
            "oi oi-laptop" => Icons.Material.Filled.Laptop,
            "oi oi-box" => Icons.Material.Filled.Inventory2,
            "oi oi-calculator" => Icons.Material.Filled.Calculate,
            "oi oi-monitor" => Icons.Material.Filled.Monitor,
            "oi oi-document" => Icons.Material.Filled.Description,
            "oi oi-bar-chart" => Icons.Material.Filled.BarChart,
            "oi oi-person" => Icons.Material.Filled.Person,
            _ => Icons.Material.Filled.ChevronRight
        };
    }
}
