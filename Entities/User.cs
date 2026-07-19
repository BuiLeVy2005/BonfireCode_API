using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Student";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Bio { get; set; } = string.Empty;

        [MaxLength(255)]
        public string AvatarUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        public string CoverUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        public string SelectedBorderUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        public string SelectedBannerUrl { get; set; } = string.Empty;

        [MaxLength(6)]
        public string? ResetToken { get; set; }
        
        public DateTime? ResetTokenExpiry { get; set; }


        // Navigation Properties
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

        // Follow System
        public virtual ICollection<User> Followers { get; set; } = new List<User>();
        public virtual ICollection<User> Following { get; set; } = new List<User>();

        // Ranking System
        public int TotalEmbers { get; set; } = 0;
        
        public int? RankId { get; set; } = 1; // Default to Rank 1 (Kẻ Lưu Đày)
        
        [ForeignKey(nameof(RankId))]
        public virtual Rank? Rank { get; set; }
    }
}
