using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Participants
{
    public class CreateParticipantRequest
    {
        [Required, MaxLength(30)]
        public string ParticipantId { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? InstId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? Zipcode { get; set; }

        [MaxLength(100)]
        public string? Person { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(30)]
        public string? Fax { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(1)]
        public string? VirtualAccount { get; set; }

        [MaxLength(30)]
        public string? AccNumber { get; set; }

        public bool Active { get; set; } = true;

        [MaxLength(100)]
        public string? VaName { get; set; }
    }
}
