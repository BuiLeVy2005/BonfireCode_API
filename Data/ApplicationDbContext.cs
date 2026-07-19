using Microsoft.EntityFrameworkCore;
using Entities;

namespace Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Rank> Ranks { get; set; }
        public DbSet<EmberTransaction> EmberTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình Follow System (N-N tự tham chiếu)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Following)
                .WithMany(u => u.Followers)
                .UsingEntity(j => j.ToTable("UserFollows"));

            // Ngăn chặn Cascade Delete Multiple Paths cho Comment
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Project)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ngăn chặn Cascade Delete Multiple Paths cho Rating
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Project)
                .WithMany(p => p.Ratings)
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed dữ liệu mặc định cho Categories
            modelBuilder.Entity<Category>().HasData(
                // Ngôn ngữ lập trình
                new Category { Id = 1, Name = "C# / .NET", Type = "NgonNgu" },
                new Category { Id = 2, Name = "PHP", Type = "NgonNgu" },
                new Category { Id = 3, Name = "Java", Type = "NgonNgu" },
                new Category { Id = 4, Name = "JavaScript / React", Type = "NgonNgu" },
                new Category { Id = 5, Name = "Python", Type = "NgonNgu" },
                new Category { Id = 6, Name = "C / C++", Type = "NgonNgu" },
                new Category { Id = 7, Name = "Go", Type = "NgonNgu" },
                new Category { Id = 8, Name = "Ruby", Type = "NgonNgu" },
                new Category { Id = 9, Name = "Swift", Type = "NgonNgu" },
                new Category { Id = 10, Name = "Kotlin", Type = "NgonNgu" },
                new Category { Id = 11, Name = "Rust", Type = "NgonNgu" },
                new Category { Id = 12, Name = "TypeScript", Type = "NgonNgu" },
                new Category { Id = 13, Name = "SQL", Type = "NgonNgu" },
                new Category { Id = 14, Name = "Dart / Flutter", Type = "NgonNgu" },
                new Category { Id = 15, Name = "Ngôn ngữ Khác", Type = "NgonNgu" },

                // Môn học
                new Category { Id = 16, Name = "Nhập môn lập trình", Type = "MonHoc" },
                new Category { Id = 17, Name = "Kỹ thuật lập trình", Type = "MonHoc" },
                new Category { Id = 18, Name = "Lập trình hướng đối tượng (OOP)", Type = "MonHoc" },
                new Category { Id = 19, Name = "Cấu trúc dữ liệu và giải thuật", Type = "MonHoc" },
                new Category { Id = 20, Name = "Cơ sở dữ liệu", Type = "MonHoc" },
                new Category { Id = 21, Name = "Phân tích thiết kế hệ thống", Type = "MonHoc" },
                new Category { Id = 22, Name = "Phát triển ứng dụng Web", Type = "MonHoc" },
                new Category { Id = 23, Name = "Phát triển ứng dụng Di động", Type = "MonHoc" },
                new Category { Id = 24, Name = "Trí tuệ nhân tạo / Học máy", Type = "MonHoc" },
                new Category { Id = 25, Name = "Đồ án / Khóa luận tốt nghiệp", Type = "MonHoc" },
                new Category { Id = 26, Name = "Môn học Khác", Type = "MonHoc" }
            );

            // Seed dữ liệu mặc định cho Ranks
            modelBuilder.Entity<Rank>().HasData(
                new Rank { Id = 1, Name = "Kẻ Lưu Đày", RequiredEmbers = 0, SvgIcon = "rank1" },
                new Rank { Id = 2, Name = "Kẻ Nhóm Lửa", RequiredEmbers = 50, SvgIcon = "rank2" },
                new Rank { Id = 3, Name = "Kỵ Sĩ Thuật Toán", RequiredEmbers = 120, SvgIcon = "rank3" },
                new Rank { Id = 4, Name = "Ma Tôn Dữ Liệu", RequiredEmbers = 250, SvgIcon = "rank4" },
                new Rank { Id = 5, Name = "Lãnh Chúa Tro Tàn", RequiredEmbers = 500, SvgIcon = "rank5" }
            );

            // Seed dữ liệu LordAdmin (Tòa Án Tối Cao)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Username = "LordAdmin",
                    Email = "admin@bonfirecode.com",
                    Role = "Admin",
                    PasswordHash = "$2a$11$T2bGDzWt7VtIJerTTo2gIuoXReuPtF51Mu9bovy4aAk50bLaI0jxG",
                    TotalEmbers = 9999,
                    RankId = 5,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    FullName = "Tòa Án Tối Cao",
                    Bio = "Người duy trì trật tự của BonfireCode.",
                    AvatarUrl = "https://placehold.co/400x400/0a0a0c/d4af37?text=LordAdmin"
                }
            );
        }
    }
}
