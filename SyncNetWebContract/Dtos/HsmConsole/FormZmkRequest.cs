using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.HsmConsole
{
    public class FormZmkRequest
    {
        [Required, MinLength(1)]
        public List<string> Components { get; set; } = new();

        public string InputMode { get; set; } = "clear";
    }
}
