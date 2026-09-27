using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Menus
{
    public class CreateMenuRequest
    {
        /// <summary>dashboard_menu.menu_id is not DB-auto-increment in the real schema — the
        /// admin picks the id (the UI prefills max(menu_id)+1 as a suggestion, but it stays
        /// editable). Matches legacy behavior, not a bug to "fix".</summary>
        [Required, Range(1, int.MaxValue)]
        public int MenuId { get; set; }

        [Required]
        public string Level1 { get; set; } = string.Empty;

        [Required]
        public string Level2 { get; set; } = string.Empty;

        public string? Level3 { get; set; }
        public string? Level4 { get; set; }
        public string? Icon { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;
    }
}
