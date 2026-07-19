using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // VD: C#, PHP

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // VD: NgonNgu, MonHoc

        // Navigation Properties
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
