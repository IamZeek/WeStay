using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeStay.MessagingService.DTOs;
using WeStay.MessagingService.Services.Interfaces;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace WeStay.MessagingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IConversationService _conversationService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(
            IMessageService messageService,
            IConversationService conversationService,
            ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _conversationService = conversationService;
            _logger = logger;
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetConversationMessages(int conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
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

                var messages = await _messageService.GetConversationMessagesAsync(conversationId, userId, page, pageSize);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for conversation {ConversationId} by user {UserId}",
                    conversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while retrieving messages" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessage(int id)
        {
            try
            {
                var message = await _messageService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound(new { Message = "Message not found" });
                }

                var userId = GetUserId();

                // Verify user has access to this conversation
                var hasAccess = await _conversationService.IsUserParticipantAsync(message.ConversationId, userId);
                if (!hasAccess)
                {
                    return Forbid();
                }

                return Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message {MessageId} by user {UserId}", id, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while retrieving the message" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid message data", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var userId = GetUserId();

                // Verify user has access to this conversation
                var hasAccess = await _conversationService.IsUserParticipantAsync(request.ConversationId, userId);
                if (!hasAccess)
                {
                    return Forbid();
                }

                var message = await _messageService.CreateMessageAsync(
                    request.ConversationId, userId, request.Content, request.MessageType);

                return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to conversation {ConversationId} by user {UserId}",
                    request.ConversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while sending the message" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMessage(int id, [FromBody] UpdateMessageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid message data", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var message = await _messageService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound(new { Message = "Message not found" });
                }

                var userId = GetUserId();

                // Verify user is the sender of the message
                if (message.SenderId != userId)
                {
                    return Forbid();
                }

                var updatedMessage = await _messageService.UpdateMessageAsync(id, request.Content);
                return Ok(updatedMessage);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Message not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating message {MessageId} by user {UserId}", id, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while updating the message" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            try
            {
                var message = await _messageService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound(new { Message = "Message not found" });
                }

                var userId = GetUserId();

                // Verify user is the sender of the message
                if (message.SenderId != userId)
                {
                    return Forbid();
                }

                var success = await _messageService.DeleteMessageAsync(id);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to delete message" });
                }

                return Ok(new { Message = "Message deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId} by user {UserId}", id, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while deleting the message" });
            }
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var message = await _messageService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    return NotFound(new { Message = "Message not found" });
                }

                var userId = GetUserId();

                // Verify user has access to this conversation
                var hasAccess = await _conversationService.IsUserParticipantAsync(message.ConversationId, userId);
                if (!hasAccess)
                {
                    return Forbid();
                }

                var success = await _messageService.MarkMessageAsReadAsync(id, userId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to mark message as read" });
                }

                return Ok(new { Message = "Message marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read by user {UserId}", id, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while marking message as read" });
            }
        }

        [HttpPost("conversation/{conversationId}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(int conversationId)
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

                var success = await _messageService.MarkAllAsReadAsync(conversationId, userId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to mark all messages as read" });
                }

                return Ok(new { Message = "All messages marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all messages as read in conversation {ConversationId} by user {UserId}",
                    conversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while marking messages as read" });
            }
        }

        [HttpGet("conversation/{conversationId}/count")]
        public async Task<IActionResult> GetMessageCount(int conversationId)
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

                var count = await _messageService.GetMessageCountAsync(conversationId);
                return Ok(new { MessageCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message count for conversation {ConversationId} by user {UserId}",
                    conversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while getting message count" });
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }

    public class UpdateMessageRequest
    {
        [Required]
        [MaxLength(5000)]
        public string Content { get; set; }
    }
}