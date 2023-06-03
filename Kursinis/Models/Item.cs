using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kursinis.Models
{
    public class Item
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

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
