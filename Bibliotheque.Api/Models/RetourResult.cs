using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Models
{
    [Keyless]
    public class RetourResult
    {
        public string Result { get; set; } = null!; // "OK"
    }
}
