using ocr_captcha.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureServices((hostContext, services) =>
{
    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddScoped<ICaptchaService, CaptchaService>();
});


var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
