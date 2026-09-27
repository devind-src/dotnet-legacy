using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardProducts
{
    /// <summary>Unlike most other Card modules, the legacy Product Master form does not mark
    /// any field readonly on edit — Issuer/Product/PanPrefix/PanLength/CardType all stay
    /// editable after create.</summary>
    public class UpdateCardProductRequest
    {
        [Required, MaxLength(30)]
        public string Issuer { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Product { get; set; } = string.Empty;

        [Required, MaxLength(12)]
        public string PanPrefix { get; set; } = string.Empty;

        [Required, Range(1, short.MaxValue)]
        public short PanLength { get; set; }

        [Required, MaxLength(30)]
        public string CardType { get; set; } = string.Empty;
    }
}
