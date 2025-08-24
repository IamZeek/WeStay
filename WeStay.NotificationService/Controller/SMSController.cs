using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeStay.NotificationService.DTOs;
using WeStay.NotificationService.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace WeStay.NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SMSController : ControllerBase
    {
        private readonly ISMSService _smsService;
        private readonly ILogger<SMSController> _logger;

        public SMSController(ISMSService smsService, ILogger<SMSController> logger)
        {
            _smsService = smsService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendSMS([FromBody] SendSMSRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid SMS request", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var success = await _smsService.SendSMSAsync(request.PhoneNumber, request.Message);

                if (success)
                {
                    return Ok(new { Message = "SMS sent successfully" });
                }
                else
                {
                    return StatusCode(500, new { Message = "Failed to send SMS" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", request.PhoneNumber);
                return StatusCode(500, new { Message = "An error occurred while sending SMS" });
            }
        }

        [HttpPost("send-verification")]
        [AllowAnonymous]
        public async Task<IActionResult> SendVerificationSMS([FromBody] SendVerificationSMSRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid verification request", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var success = await _smsService.SendVerificationSMSAsync(request.PhoneNumber, request.VerificationCode);

                if (success)
                {
                    return Ok(new { Message = "Verification SMS sent successfully" });
                }
                else
                {
                    return StatusCode(500, new { Message = "Failed to send verification SMS" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification SMS to {PhoneNumber}", request.PhoneNumber);
                return StatusCode(500, new { Message = "An error occurred while sending verification SMS" });
            }
        }

        [HttpPost("validate-phone")]
        public async Task<IActionResult> ValidatePhoneNumber([FromBody] ValidatePhoneRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid phone number", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var isValid = await _smsService.ValidatePhoneNumberAsync(request.PhoneNumber);

                return Ok(new { IsValid = isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating phone number: {PhoneNumber}", request.PhoneNumber);
                return StatusCode(500, new { Message = "An error occurred while validating phone number" });
            }
        }

        [HttpPost("format-phone")]
        public async Task<IActionResult> FormatPhoneNumber([FromBody] FormatPhoneRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid phone number", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                var formattedNumber = await _smsService.FormatPhoneNumberAsync(request.PhoneNumber);

                return Ok(new { FormattedNumber = formattedNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting phone number: {PhoneNumber}", request.PhoneNumber);
                return StatusCode(500, new { Message = "An error occurred while formatting phone number" });
            }
        }
    }

    public class SendSMSRequest
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        [MaxLength(1600)] // SMS character limit
        public string Message { get; set; }
    }

    public class SendVerificationSMSRequest
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 4)]
        public string VerificationCode { get; set; }
    }

    public class ValidatePhoneRequest
    {
        [Required]
        public string PhoneNumber { get; set; }
    }

    public class FormatPhoneRequest
    {
        [Required]
        public string PhoneNumber { get; set; }
    }
}