namespace ocr_captcha.Services;

public interface ICaptchaService
{
    byte[] ProcessImage(IFormFile formFile);

    string ExtractTextFromImage(byte[] imageData);

}
