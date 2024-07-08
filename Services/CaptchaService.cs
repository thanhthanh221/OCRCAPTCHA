using OpenCvSharp;
using Tesseract;

namespace ocr_captcha.Services;

public class CaptchaService : ICaptchaService
{

    public byte[] ProcessImage(IFormFile formFile)
    {
        // Đọc nội dung của IFormFile vào Mat
        using (var memoryStream = new MemoryStream())
        {
            formFile.CopyTo(memoryStream);
            var fileData = memoryStream.ToArray();

            using var src = Mat.FromImageData(fileData);

            using var binaryMask = new Mat();

            // Màu của các đường khác với màu của chữ
            var linesColor = Scalar.FromRgb(0x70, 0x70, 0x70);

            // Tạo mặt nạ cho các đường
            Cv2.InRange(src, linesColor, linesColor, binaryMask);

            using var masked = new Mat();

            // Tạo ảnh tương ứng
            // Giãn nở các đường một chút vì sự răng cưa có thể đã làm mờ các đường viền trong quá trình tạo mặt nạ
            src.CopyTo(masked, binaryMask);
            int linesDilate = 2; // Kích thước giãn nở đã điều chỉnh
            using (var element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(linesDilate, linesDilate)))
            {
                Cv2.Dilate(masked, masked, element);
            }

            // Chuyển đổi mặt nạ sang ảnh xám
            Cv2.CvtColor(masked, masked, ColorConversionCodes.BGR2GRAY);
            using (var dst = src.EmptyClone())
            {
                // Vẽ lại các đường lớn
                Cv2.Inpaint(src, masked, dst, 3, InpaintMethod.NS);

                // Xóa bỏ các đường nhỏ
                linesDilate = 1; // Kích thước giãn nở đã điều chỉnh
                using (var element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(linesDilate, linesDilate)))
                {
                    Cv2.Dilate(dst, dst, element);
                }

                // Làm mờ Gaussian với kích thước kernel nhỏ hơn
                Cv2.GaussianBlur(dst, dst, new Size(1, 1), 2); // Kích thước kernel đã điều chỉnh

                // Thêm unsharp masking với các thông số đã điều chỉnh
                Cv2.AddWeighted(dst, 1.2, dst, -0.2, 0, dst); // Các thông số đã điều chỉnh

                // Tạo một Mat mới cho việc lọc hai chiều (bilateral filtering)
                using (var dstFiltered = new Mat())
                {
                    // Lọc hai chiều với các thông số đã điều chỉnh
                    Cv2.BilateralFilter(dst, dstFiltered, 5, 30, 30); // Các thông số đã điều chỉnh

                    // Chuyển đổi sang ảnh xám
                    Cv2.CvtColor(dstFiltered, dstFiltered, ColorConversionCodes.BGR2GRAY);

                    // Áp dụng adaptive thresholding để tăng độ rõ nét của chữ
                    Cv2.AdaptiveThreshold(dstFiltered, dstFiltered, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 15, 10);

                    // Áp dụng erosion và dilation để tách biệt các ký tự
                    int morphSize = 1; // Kích thước cho các phép biến đổi hình thái học
                    using (var element = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(morphSize, morphSize)))
                    {
                        Cv2.Erode(dstFiltered, dstFiltered, element, iterations: 1); // Giảm số lần xói mòn
                        Cv2.Dilate(dstFiltered, dstFiltered, element, iterations: 2); // Giảm số lần giãn nở
                    }

                    // Thêm bước unsharp masking để tăng độ nét
                    Cv2.AddWeighted(dstFiltered, 1.5, dstFiltered, -0.5, 0, dstFiltered);

                    // Làm mịn ảnh với GaussianBlur hoặc BilateralFilter
                    Cv2.GaussianBlur(dstFiltered, dstFiltered, new Size(3, 3), 0); // Thêm GaussianBlur để làm mịn ảnh
                                                                                   // Hoặc sử dụng BilateralFilter nếu cần
                                                                                   // Cv2.BilateralFilter(dstFiltered, dstFiltered, 9, 75, 75);

                    // Tạo nền trắng
                    int borderSize = 15; // Điều chỉnh kích thước viền theo nhu cầu
                    var whiteBackground = new Mat(dstFiltered.Rows + borderSize * 2, dstFiltered.Cols + borderSize * 2, MatType.CV_8UC1, Scalar.White);

                    // Đặt ảnh đã xử lý lên nền trắng
                    var roi = new OpenCvSharp.Rect(borderSize, borderSize, dstFiltered.Cols, dstFiltered.Rows);
                    dstFiltered.CopyTo(new Mat(whiteBackground, roi));

                    // Mã hóa Mat thành mảng byte[]
                    using var resultStream = new MemoryStream();

                    whiteBackground.WriteToStream(resultStream, ".tif");
                    return resultStream.ToArray();
                }
            }
        }
    }

    public string ExtractTextFromImage(byte[] imageData)
    {
        string traineddataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "traineddata");

        // Tạo TesseractEngine
        using (var engine = new TesseractEngine(traineddataPath, "eng", EngineMode.Default))
        {
            // Thiết lập các ký tự whitelist
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890");

            // Chuyển đổi mảng byte[] thành Pix
            using (var memoryStream = new MemoryStream(imageData))
            {
                using var img = Pix.LoadFromMemory(memoryStream.ToArray());
                // Xử lý ảnh với Tesseract
                using var page = engine.Process(img);
                return page.GetText().Trim();
            }
        }
    }

}
