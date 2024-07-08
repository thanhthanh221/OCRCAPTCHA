using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ocr_captcha.Services;

namespace ocr_captcha.Controllers;

[ApiController]
[Route("api/ocrcaptcha")]
public class OcrCaptchaController : ControllerBase
{

    private readonly ILogger<OcrCaptchaController> _logger;
    private readonly ICaptchaService _captchaService;

    public OcrCaptchaController(
        ILogger<OcrCaptchaController> logger,
        ICaptchaService captchaService)
    {
        _logger = logger;
        _captchaService = captchaService;
    }

    [HttpPost]
    [Route("imagetotext")]
    public IActionResult OcrImagetoTextAsync([Required] IFormFile captcha)
    {
        try
        {
            var imageProcess = _captchaService.ProcessImage(captcha);

            var textCaptcha = _captchaService.ExtractTextFromImage(imageProcess);

            return Ok(textCaptcha);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
