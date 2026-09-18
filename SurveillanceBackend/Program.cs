var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

var captureDirectory = Path.Combine(app.Environment.ContentRootPath, "captures");
Directory.CreateDirectory(captureDirectory);

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

    var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
    var filePath = Path.Combine(captureDirectory, $"motion-{timestamp}.jpg");
    await using (var output = File.Create(filePath))
    {
        await image.CopyToAsync(output, cancellationToken);
    }

    return Results.Ok(new { fileName = Path.GetFileName(filePath) });
});

app.Run();
