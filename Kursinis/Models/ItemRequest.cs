using System.ComponentModel.DataAnnotations;

namespace Kursinis.Models
{
    public class ItemRequest
    {
        [Required]
        public double Price { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int LocationId { get; set; }

        [Required]
        public bool RequiresAuthorizedUser { get; set; }
    }
}
