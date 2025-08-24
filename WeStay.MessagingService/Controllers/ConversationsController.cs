using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeStay.MessagingService.DTOs;
using WeStay.MessagingService.Services.Interfaces;
using System.Security.Claims;

namespace WeStay.MessagingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<ConversationsController> _logger;

        public ConversationsController(
            IConversationService conversationService,
            ILogger<ConversationsController> logger)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserConversations()
        {
            try
            {
                var userId = GetUserId();
                var conversations = await _conversationService.GetUserConversationsAsync(userId);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user {UserId}", GetUserId());
                return StatusCode(500, new { Message = "An error occurred while retrieving conversations" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetConversation(int id)
        {
            try
            {
                var userId = GetUserId();

                // Verify user has access to this conversation
                var hasAccess = await _conversationService.IsUserParticipantAsync(id, userId);
                if (!hasAccess)
                {
                    return Forbid();
                }

                var conversation = await _conversationService.GetConversationByIdAsync(id);
                if (conversation == null)
                {
                    return NotFound(new { Message = "Conversation not found" });
                }

                return Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation {ConversationId} for user {UserId}", id, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while retrieving the conversation" });
            }
        }

        [HttpGet("guid/{guid}")]
        public async Task<IActionResult> GetConversationByGuid(Guid guid)
        {
            try
            {
                var userId = GetUserId();
                var conversation = await _conversationService.GetConversationByGuidAsync(guid);

                if (conversation == null)
                {
                    return NotFound(new { Message = "Conversation not found" });
                }

                // Verify user has access to this conversation
                var hasAccess = await _conversationService.IsUserParticipantAsync(conversation.Id, userId);
                if (!hasAccess)
                {
                    return Forbid();
                }

                return Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation by GUID {Guid} for user {UserId}", guid, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while retrieving the conversation" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid conversation data", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var userId = GetUserId();

                // Ensure the current user is included in participants
                if (!request.ParticipantIds.Contains(userId))
                {
                    request.ParticipantIds.Add(userId);
                }

                var conversation = await _conversationService.CreateConversationAsync(request);
                return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation for user {UserId}", GetUserId());
                return StatusCode(500, new { Message = "An error occurred while creating the conversation" });
            }
        }

        [HttpPost("{conversationId}/participants")]
        public async Task<IActionResult> AddParticipant(int conversationId, [FromBody] AddParticipantRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid request data", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var userId = GetUserId();

                // Verify user has access to this conversation
                var hasAccess = await _conversationService.IsUserParticipantAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return Forbid();
                }

                var success = await _conversationService.AddParticipantAsync(conversationId, request.UserId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to add participant" });
                }

                return Ok(new { Message = "Participant added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding participant to conversation {ConversationId} by user {UserId}",
                    conversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while adding participant" });
            }
        }

        [HttpDelete("{conversationId}/participants/{participantId}")]
        public async Task<IActionResult> RemoveParticipant(int conversationId, int participantId)
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

                var success = await _conversationService.RemoveParticipantAsync(conversationId, participantId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to remove participant" });
                }

                return Ok(new { Message = "Participant removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing participant from conversation {ConversationId} by user {UserId}",
                    conversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while removing participant" });
            }
        }

        [HttpPost("{conversationId}/read")]
        public async Task<IActionResult> MarkAsRead(int conversationId)
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

                var success = await _conversationService.MarkConversationAsReadAsync(conversationId, userId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to mark conversation as read" });
                }

                return Ok(new { Message = "Conversation marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking conversation {ConversationId} as read by user {UserId}",
                    conversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while marking conversation as read" });
            }
        }

        [HttpPost("{conversationId}/archive")]
        public async Task<IActionResult> ArchiveConversation(int conversationId)
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

                var success = await _conversationService.ArchiveConversationAsync(conversationId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to archive conversation" });
                }

                return Ok(new { Message = "Conversation archived successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving conversation {ConversationId} by user {UserId}",
                    conversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while archiving conversation" });
            }
        }

        [HttpGet("{conversationId}/unread-count")]
        public async Task<IActionResult> GetUnreadCount(int conversationId)
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

                var count = await _conversationService.GetUnreadCountAsync(conversationId, userId);
                return Ok(new { UnreadCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for conversation {ConversationId} by user {UserId}",
                    conversationId, GetUserId());
                return StatusCode(500, new { Message = "An error occurred while getting unread count" });
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }
}