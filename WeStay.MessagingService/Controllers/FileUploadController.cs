using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeStay.MessagingService.Services.Interfaces;
using System.Security.Claims;

namespace WeStay.MessagingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IConversationService _conversationService;
        private readonly ILogger<FileUploadController> _logger;
        private readonly IWebHostEnvironment _environment;

        public FileUploadController(
            IMessageService messageService,
            IConversationService conversationService,
            ILogger<FileUploadController> logger,
            IWebHostEnvironment environment)
        {
            _messageService = messageService;
            _conversationService = conversationService;
            _logger = logger;
            _environment = environment;
        }

        [HttpPost("message/{conversationId}")]
        public async Task<IActionResult> UploadFile(int conversationId, IFormFile file)
        {
            try
            {
                var userId = GetUserId();

                // Verify user has access to this conversation
                var hasAccess = await _conversationService.IsUserParticipantAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return Forbid();
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Message = "No file provided" });
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { Message = "File size must be less than 10MB" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { Message = "File type not allowed" });
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "messages");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Determine message type based on file extension
                var messageType = GetMessageTypeFromExtension(fileExtension);

                // Create file URL
                var fileUrl = $"/uploads/messages/{fileName}";

                // Create message with file information
                var messageContent = messageType == "image"
                    ? $"📷 {file.FileName}"
                    : $"📎 {file.FileName}";

                var message = await _messageService.CreateMessageAsync(
                    conversationId, userId, messageContent, messageType);

                // In a real implementation, you'd update the message with file information
                // For now, we'll return the file information

                return Ok(new
                {
                    Message = "File uploaded successfully",
                    FileName = file.FileName,
                    FileUrl = fileUrl,
                    FileSize = file.Length,
                    MessageId = message.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to conversation {ConversationId} by user {UserId}",
                    conversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while uploading the file" });
            }
        }

        [HttpGet("download/{filename}")]
        [AllowAnonymous]
        public IActionResult DownloadFile(string filename)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", "messages", filename);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { Message = "File not found" });
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = GetContentType(Path.GetExtension(filename).ToLower());

                return File(fileBytes, contentType, filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileName}", filename);
                return StatusCode(500, new { Message = "An error occurred while downloading the file" });
            }
        }

        private string GetMessageTypeFromExtension(string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" => "image",
                _ => "file"
            };
        }

        private string GetContentType(string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }
}