namespace ToyBoxApi.Services;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file, string folder);
    void DeleteFile(string? fileUrl);
}

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;

    public FileService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folder)
    {
        var uploadPath = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadPath);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        if (!allowed.Contains(ext))
            throw new InvalidOperationException("Only image files are allowed.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("File size must be under 5MB.");

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Save relative path only
        return $"/uploads/{folder}/{fileName}";
    }

    public void DeleteFile(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        string relativePath = fileUrl;

        // If old image has full URL, convert it to relative path
        if (relativePath.StartsWith("http://") || relativePath.StartsWith("https://"))
        {
            var uri = new Uri(relativePath);
            relativePath = uri.AbsolutePath;
        }

        relativePath = relativePath.TrimStart('/');

        var fullPath = Path.Combine(_env.WebRootPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}