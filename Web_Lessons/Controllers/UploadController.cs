using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;

namespace Web_Lessons.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
        {
            _env = env;
            _logger = logger;
        }

        private string TempPath => Path.Combine(_env.WebRootPath, "uploads", "temp");
        private string FinalPath => Path.Combine(_env.WebRootPath, "uploads", "videos");

        // استقبال الجزء من الفيديو
        [HttpPost("chunk")]
        [RequestSizeLimit(2147483648)] // 2GB
        [RequestFormLimits(MultipartBodyLengthLimit = 2147483648)]
        public async Task<IActionResult> UploadChunk(
            [FromForm] IFormFile chunk,
            [FromForm] string fileId,
            [FromForm] string fileName,
            [FromForm] int chunkIndex,
            [FromForm] int totalChunks)
        {
            try
            {
                _logger.LogInformation($"Receiving chunk {chunkIndex + 1}/{totalChunks} for file {fileName}");

                if (chunk == null || chunk.Length == 0)
                    return BadRequest(new { error = "No chunk received" });

                // التأكد من وجود مجلد التخزين المؤقت
                Directory.CreateDirectory(TempPath);

                // إنشاء اسم الملف المؤقت
                var tempFileName = $"{fileId}_{chunkIndex}.part";
                var tempFilePath = Path.Combine(TempPath, tempFileName);

                // حفظ الجزء
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await chunk.CopyToAsync(stream);
                }

                // حفظ بيانات الملف (للدمج لاحقًا)
                var infoFile = Path.Combine(TempPath, $"{fileId}.info");
                await System.IO.File.WriteAllTextAsync(infoFile,
                    $"FileName={fileName}\nTotalChunks={totalChunks}\nOriginalName={fileName}");

                return Ok(new
                {
                    success = true,
                    chunkIndex,
                    message = $"Chunk {chunkIndex + 1}/{totalChunks} uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading chunk");
                return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
            }
        }

        // دمج الأجزاء وإنشاء الفيديو النهائي
        [HttpPost("complete")]
        public async Task<IActionResult> CompleteUpload([FromBody] UploadCompleteModel model)
        {
            try
            {
                _logger.LogInformation($"Completing upload for file {model.FileName}");

                Directory.CreateDirectory(FinalPath);

                // قراءة معلومات الملف
                var infoFile = Path.Combine(TempPath, $"{model.FileId}.info");
                if (!System.IO.File.Exists(infoFile))
                    return NotFound(new { error = "File info not found" });

                var infoLines = await System.IO.File.ReadAllLinesAsync(infoFile);
                var fileName = infoLines.FirstOrDefault(l => l.StartsWith("FileName="))?.Split('=')[1];
                var totalChunksStr = infoLines.FirstOrDefault(l => l.StartsWith("TotalChunks="))?.Split('=')[1];

                if (string.IsNullOrEmpty(fileName) || !int.TryParse(totalChunksStr, out int totalChunks))
                    return BadRequest(new { error = "Invalid file info" });

                // إنشاء اسم فريد للفيديو النهائي
                var fileExtension = Path.GetExtension(fileName);
                var finalFileName = $"{Guid.NewGuid()}{fileExtension}";
                var finalFilePath = Path.Combine(FinalPath, finalFileName);

                // دمج جميع الأجزاء
                using (var finalStream = new FileStream(finalFilePath, FileMode.Create))
                {
                    for (int i = 0; i < totalChunks; i++)
                    {
                        var chunkPath = Path.Combine(TempPath, $"{model.FileId}_{i}.part");
                        if (!System.IO.File.Exists(chunkPath))
                        {
                            return BadRequest(new { error = $"Chunk {i} not found" });
                        }

                        using (var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read))
                        {
                            await chunkStream.CopyToAsync(finalStream);
                        }

                        // حذف الجزء بعد دمجه
                        System.IO.File.Delete(chunkPath);
                    }
                }

                // حذف ملف المعلومات
                System.IO.File.Delete(infoFile);

                // المسار العام للوصول للفيديو
                var publicPath = $"/uploads/videos/{finalFileName}";

                _logger.LogInformation($"File uploaded successfully: {publicPath}");

                return Ok(new
                {
                    success = true,
                    path = publicPath,
                    fileName = finalFileName,
                    originalName = fileName,
                    size = new FileInfo(finalFilePath).Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing upload");
                return StatusCode(500, new { error = $"Failed to complete upload: {ex.Message}" });
            }
        }

        // التحقق من حالة الرفع (إذا كان هناك أجزاء مرفوعة بالفعل)
        [HttpGet("check")]
        public IActionResult CheckUpload([FromQuery] string fileId)
        {
            try
            {
                var uploadedChunks = new List<int>();
                var infoFile = Path.Combine(TempPath, $"{fileId}.info");

                if (System.IO.File.Exists(infoFile))
                {
                    for (int i = 0; i < 1000; i++) // فرضياً 1000 جزء كحد أقصى
                    {
                        var chunkPath = Path.Combine(TempPath, $"{fileId}_{i}.part");
                        if (System.IO.File.Exists(chunkPath))
                        {
                            uploadedChunks.Add(i);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return Ok(new
                {
                    uploadedChunks,
                    hasInfo = System.IO.File.Exists(infoFile),
                    totalUploaded = uploadedChunks.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking upload status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // إلغاء الرفع وحذف الأجزاء المؤقتة
        [HttpPost("cancel")]
        public IActionResult CancelUpload([FromBody] UploadCancelModel model)
        {
            try
            {
                var filesDeleted = 0;

                // حذف جميع الأجزاء
                for (int i = 0; i < 1000; i++)
                {
                    var chunkPath = Path.Combine(TempPath, $"{model.FileId}_{i}.part");
                    if (System.IO.File.Exists(chunkPath))
                    {
                        System.IO.File.Delete(chunkPath);
                        filesDeleted++;
                    }
                    else
                    {
                        break;
                    }
                }

                // حذف ملف المعلومات
                var infoFile = Path.Combine(TempPath, $"{model.FileId}.info");
                if (System.IO.File.Exists(infoFile))
                {
                    System.IO.File.Delete(infoFile);
                }

                _logger.LogInformation($"Cancelled upload for {model.FileId}, deleted {filesDeleted} chunks");

                return Ok(new
                {
                    success = true,
                    message = $"Upload cancelled, {filesDeleted} chunks deleted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling upload");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // دالة مساعدة للحصول على نوع الملف
        [HttpGet("mime-type")]
        public IActionResult GetMimeType([FromQuery] string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return Ok(new { mimeType = contentType });
        }
    }

    public class UploadCompleteModel
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public int TotalChunks { get; set; }
    }

    public class UploadCancelModel
    {
        public string FileId { get; set; }
    }
}