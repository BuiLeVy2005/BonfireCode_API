using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Entities
{
    public class Rank
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int RequiredEmbers { get; set; }

        public string SvgIcon { get; set; } = string.Empty;

        // Navigation Property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
