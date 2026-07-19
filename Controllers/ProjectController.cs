using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Data;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeShareAPI.Services;

namespace GiuaKy.Controllers
{
    [Route("api/projects")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IRankService _rankService;

        public ProjectController(ApplicationDbContext context, IWebHostEnvironment env, IRankService rankService)
        {
            _context = context;
            _env = env;
            _rankService = rankService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateProject([FromForm] CreateProjectRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Lấy UserId từ JWT Token Claims (Hỗ trợ nhiều kiểu map của .NET 8)
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                            ?? User.FindFirstValue("nameid");

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                var allClaims = string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
                return Unauthorized(new { Message = "Không thể xác thực người dùng. Claims: " + allClaims });
            }

            // Kiểm tra Category có tồn tại không
            var dbCategories = await _context.Categories.Where(c => request.CategoryIds.Contains(c.Id)).ToListAsync();
            if (dbCategories.Count == 0 && request.CategoryIds.Count > 0)
            {
                return BadRequest("Categories không hợp lệ.");
            }

            // Tạo thư mục lưu trữ nếu chưa có
            var storagePath = Path.Combine(_env.ContentRootPath, "storage");
            var imagesPath = Path.Combine(storagePath, "images");
            var sourcesPath = Path.Combine(storagePath, "sources");

            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);
            if (!Directory.Exists(sourcesPath)) Directory.CreateDirectory(sourcesPath);

            string? thumbnailUrl = null;
            if (request.ThumbnailFile != null && request.ThumbnailFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.ThumbnailFile.FileName);
                var filePath = Path.Combine(imagesPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ThumbnailFile.CopyToAsync(stream);
                }
                thumbnailUrl = "/storage/images/" + fileName;
            }

            string sourceCodeUrl = string.Empty;
            if (request.SourceCodeFile != null && request.SourceCodeFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.SourceCodeFile.FileName);
                var filePath = Path.Combine(sourcesPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.SourceCodeFile.CopyToAsync(stream);
                }
                sourceCodeUrl = "/storage/sources/" + fileName;
            }
            else
            {
                return BadRequest("Vui lòng đính kèm SourceCodeFile.");
            }

            var project = new Project
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                Categories = dbCategories,
                UserId = userId,
                SourceCodeUrl = sourceCodeUrl,
                ThumbnailUrl = thumbnailUrl ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Reward Embers
            await _rankService.AddEmbersAsync(userId, "CREATE_PROJECT", project.Id, 50);


            return Created($"/api/projects/{project.Id}", new { Message = "Upload thành công", ProjectId = project.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] string? title, [FromQuery] int? categoryId)
        {
            var query = _context.Projects
                .Include(p => p.User)
                .Include(p => p.Categories)
                .AsQueryable();

            if (!string.IsNullOrEmpty(title))
            {
                query = query.Where(p => p.Title.Contains(title));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.Categories.Any(c => c.Id == categoryId.Value));
            }

            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Description,
                    p.ThumbnailUrl,
                    p.SourceCodeUrl,
                    p.CreatedAt,
                    CategoryNames = p.Categories.Select(c => c.Name).ToList(),
                    AuthorName = p.User.Username,
                    AuthorAvatarUrl = p.User.AvatarUrl,
                    DownloadCount = p.DownloadCount
                })
                .ToListAsync();

            return Ok(projects);
        }

        [HttpGet("liked")]
        [Authorize]
        public async Task<IActionResult> GetLikedProjects()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                            ?? User.FindFirstValue("nameid");

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            }

            var likedProjects = await _context.Ratings
                .Where(r => r.UserId == userId)
                .Include(r => r.Project)
                .ThenInclude(p => p.User)
                .Include(r => r.Project)
                .ThenInclude(p => p.Categories)
                .Select(r => new
                {
                    r.Project.Id,
                    r.Project.Title,
                    r.Project.Description,
                    r.Project.ThumbnailUrl,
                    r.Project.SourceCodeUrl,
                    r.Project.CreatedAt,
                    CategoryNames = r.Project.Categories.Select(c => c.Name).ToList(),
                    AuthorName = r.Project.User.Username,
                    AuthorAvatarUrl = r.Project.User.AvatarUrl,
                    DownloadCount = r.Project.DownloadCount
                })
                .ToListAsync();

            return Ok(likedProjects);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProjectById(Guid id)
        {
            var project = await _context.Projects
                .Include(p => p.User)
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound(new { Message = "Project không tồn tại." });
            }

            return Ok(new
            {
                project.Id,
                project.Title,
                project.Description,
                project.ThumbnailUrl,
                project.SourceCodeUrl,
                project.CreatedAt,
                CategoryNames = project.Categories.Select(c => c.Name).ToList(),
                AuthorName = project.User.Username,
                AuthorAvatarUrl = project.User.AvatarUrl,
                DownloadCount = project.DownloadCount
            });
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                            ?? User.FindFirstValue("nameid");

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            }

            var project = await _context.Projects.Include(p => p.Categories).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound(new { Message = "Project không tồn tại." });

            if (project.UserId != userId) return Forbid();

            project.Title = request.Title;
            if (request.Description != null) project.Description = request.Description;

            project.Categories.Clear();
            var newCategories = await _context.Categories.Where(c => request.CategoryIds.Contains(c.Id)).ToListAsync();
            foreach(var cat in newCategories)
            {
                project.Categories.Add(cat);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Cập nhật thành công." });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                            ?? User.FindFirstValue("nameid");

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized(new { Message = "Không thể xác thực người dùng." });
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound(new { Message = "Project không tồn tại." });

            if (project.UserId != userId) return Forbid();

            // Xóa file vật lý
            var storageRoot = _env.ContentRootPath;
            if (!string.IsNullOrEmpty(project.SourceCodeUrl))
            {
                var relativePath = project.SourceCodeUrl.TrimStart('/');
                var fullSourcePath = Path.Combine(storageRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullSourcePath)) System.IO.File.Delete(fullSourcePath);
            }

            if (!string.IsNullOrEmpty(project.ThumbnailUrl))
            {
                var relativePath = project.ThumbnailUrl.TrimStart('/');
                var fullThumbPath = Path.Combine(storageRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullThumbPath)) System.IO.File.Delete(fullThumbPath);
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Xóa đồ án thành công" });
        }

        [HttpPost("{id}/download")]
        public async Task<IActionResult> IncrementDownload(Guid id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound(new { Message = "Project không tồn tại." });

            project.DownloadCount += 1;
            await _context.SaveChangesAsync();

            // Award 10 Embers to the Author for getting a download
            // (Anti-spam in RankService will ensure they only get this once per project)
            await _rankService.AddEmbersAsync(project.UserId, "RECEIVE_DOWNLOAD", project.Id, 10);

            return Ok(new { Message = "Đã tăng lượt tải", DownloadCount = project.DownloadCount });
        }
    }

    public class CreateProjectRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<int> CategoryIds { get; set; } = new List<int>();
        public IFormFile SourceCodeFile { get; set; } = null!;
        public IFormFile? ThumbnailFile { get; set; }
    }

    public class UpdateProjectRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<int> CategoryIds { get; set; } = new List<int>();
    }
}
