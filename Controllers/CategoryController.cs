using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using System.Threading.Tasks;

namespace CodeShareAPI.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            // Sắp xếp theo Id hoặc Gom nhóm theo Type tuỳ theo nhu cầu Frontend
            // Ở đây trả về toàn bộ và sắp xếp theo ID, Frontend sẽ dễ dàng gom nhóm dựa trên trường Type.
            var categories = await _context.Categories
                .OrderBy(c => c.Id)
                .ToListAsync();

            return Ok(categories);
        }
    }
}
