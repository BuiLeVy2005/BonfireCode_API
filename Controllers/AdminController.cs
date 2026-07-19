using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;
using Entities;
using CodeShareAPI.Services;

namespace CodeShareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRankService _rankService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext context, IRankService rankService, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _rankService = rankService;
            _env = env;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalProjects = await _context.Projects.CountAsync();
            var totalComments = await _context.Comments.CountAsync();
            var totalDownloads = await _context.Projects.SumAsync(p => p.DownloadCount);

            return Ok(new
            {
                totalUsers,
                totalProjects,
                totalComments,
                totalDownloads
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Rank)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.TotalEmbers,
                    RankName = u.Rank != null ? u.Rank.Name : "Kẻ Lưu Đày"
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpDelete("projects/{id}")]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound(new { Message = "Dự án không tồn tại." });

            // Remove physical file
            if (!string.IsNullOrEmpty(project.SourceCodeUrl))
            {
                var relativePath = project.SourceCodeUrl.TrimStart('/');
                var physicalPath = Path.Combine(_env.ContentRootPath, relativePath.Replace("/", "\\"));
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã xóa dự án thành công." });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { Message = "User không tồn tại." });

            if (user.Role == "Admin")
            {
                return BadRequest(new { Message = "Không thể xóa Admin!" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã xóa người dùng thành công." });
        }

        public class GrantEmbersRequest
        {
            public int Points { get; set; }
        }

        [HttpPost("users/{id}/grant-embers")]
        public async Task<IActionResult> GrantEmbers(Guid id, [FromBody] GrantEmbersRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { Message = "User không tồn tại." });

            // Admin forces points, can be negative to deduct
            await _rankService.AddEmbersAsync(id, "ADMIN_GRANT", Guid.Empty, request.Points);

            return Ok(new { Message = $"Đã cấp {request.Points} Embers cho {user.Username}." });
        }
    }
}
