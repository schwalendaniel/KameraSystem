using SurveillanceBackend;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<CaptureStorage>();
builder.Services.AddHostedService<CaptureCleanupService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/captures", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("Es muss ein Multipart-Formular mit einem Bild gesendet werden.");
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var image = form.Files.GetFile("image");
    if (image is null || image.Length == 0)
    {
        return Results.BadRequest("Kein Bild empfangen.");
    }

    if (!string.Equals(image.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Es werden nur JPEG-Bilder unterstützt.");
    }

    var storage = app.Services.GetRequiredService<CaptureStorage>();
    await using var imageStream = image.OpenReadStream();
    var fileName = await storage.SaveAsync(imageStream, cancellationToken);

    return Results.Ok(new { fileName });
});

app.Run();
