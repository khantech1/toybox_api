using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Toys;

namespace ToyBoxApi.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
}

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        return await _db.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryDto
            {
                CategoryId   = c.CategoryId,
                CategoryName = c.CategoryName,
            })
            .ToListAsync();
    }
}
