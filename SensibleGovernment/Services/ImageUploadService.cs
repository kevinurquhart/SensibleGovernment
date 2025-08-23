using System.Security.Cryptography;

namespace SensibleGovernment.Services;

public class ImageUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImageUploadService> _logger;
    private readonly string _uploadPath;
    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public ImageUploadService(IWebHostEnvironment environment, ILogger<ImageUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
        _uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "images");

        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
            _logger.LogInformation($"Created upload directory: {_uploadPath}");
        }
    }

    public async Task<(bool Success, string? FilePath, string? Error)> UploadImageAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        try
        {
            // Validate file size
            if (fileStream.Length > _maxFileSize)
            {
                return (false, null, $"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");
            }

            // Validate file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return (false, null, $"File type {extension} is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Validate content type
            if (!contentType.StartsWith("image/"))
            {
                return (false, null, "File must be an image");
            }

            // Generate unique filename
            var uniqueFileName = GenerateUniqueFileName(fileName);
            var filePath = Path.Combine(_uploadPath, uniqueFileName);

            // Save file
            using (var fileOutputStream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileOutputStream);
            }

            // Return web-accessible path
            var webPath = $"/uploads/images/{uniqueFileName}";
            _logger.LogInformation($"Image uploaded successfully: {webPath}");

            return (true, webPath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return (false, null, "An error occurred while uploading the image");
        }
    }

    public async Task<(bool Success, string? ThumbnailPath, string? Error)> CreateThumbnailAsync(
        string originalImagePath,
        int maxWidth = 400,
        int maxHeight = 300)
    {
        try
        {
            // For now, we'll return the same image
            // In production, you'd use ImageSharp or similar to resize
            return (true, originalImagePath, null);

            // TODO: Implement actual thumbnail generation with ImageSharp:
            // using var image = await Image.LoadAsync(originalImagePath);
            // image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(maxWidth, maxHeight), Mode = ResizeMode.Max }));
            // await image.SaveAsync(thumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating thumbnail");
            return (false, null, "Failed to create thumbnail");
        }
    }

    public bool DeleteImage(string webPath)
    {
        try
        {
            if (string.IsNullOrEmpty(webPath))
                return false;

            var fileName = Path.GetFileName(webPath);
            var filePath = Path.Combine(_uploadPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation($"Deleted image: {filePath}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting image: {webPath}");
            return false;
        }
    }

    private string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(6))
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        return $"{timestamp}_{random}{extension}";
    }

    public class ImageUploadResult
    {
        public bool Success { get; set; }
        public string? ImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Error { get; set; }
    }
}