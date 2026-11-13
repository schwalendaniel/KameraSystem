<<<<<<< HEAD
=======
using SurveillanceBackend;
>>>>>>> c0cf6ffaac8778654cc91306d40c2c334c11e845

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<CaptureStorage>();
builder.Services.AddHostedService<CaptureCleanupService>();

// Register email service
builder.Services.AddSingleton<CameraAlertService>();

builder.Services.AddSingleton<MotionEmailBatchService>();

builder.Services.AddHostedService(
    sp => sp.GetRequiredService<MotionEmailBatchService>());

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

<<<<<<< HEAD
var captureDirectory = Path.Combine(
    app.Environment.ContentRootPath,
    "captures");

Directory.CreateDirectory(captureDirectory);

app.MapPost("/api/captures", async (
    HttpRequest request,
    MotionEmailBatchService batchService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
=======
app.MapPost("/api/captures", async (HttpRequest request, CancellationToken cancellationToken) =>
>>>>>>> c0cf6ffaac8778654cc91306d40c2c334c11e845
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(
            "Es muss ein Multipart-Formular mit einem Bild gesendet werden.");
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var image = form.Files.GetFile("image");

    if (image is null || image.Length == 0)
    {
        return Results.BadRequest("Kein Bild empfangen.");
    }

    if (!string.Equals(
        image.ContentType,
        "image/jpeg",
        StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(
            "Es werden nur JPEG-Bilder unterstützt.");
    }

<<<<<<< HEAD
    // Get camera information
    var cameraName = form["cameraName"].ToString();
    var cameraId = form["cameraId"].ToString();

    if (string.IsNullOrWhiteSpace(cameraName))
        cameraName = "Kamera 1";

    if (string.IsNullOrWhiteSpace(cameraId))
        cameraId = "1";

    // Save camera image
    var timestamp = DateTime.Now;
    var fileName = $"motion-{timestamp:yyyyMMdd-HHmmss-fff}.jpg";
    var filePath = Path.Combine(captureDirectory, fileName);

    await using (var output = File.Create(filePath))
    {
        await image.CopyToAsync(output, cancellationToken);
    }

    logger.LogInformation(
        "Kamerabild gespeichert: {FileName}", fileName);

    // Add camera image to the email batch
    batchService.AddCapture(new MotionCapture(
        cameraName,
        cameraId,
        timestamp,
        filePath));

    logger.LogInformation(
        "Kamerabild zur E-Mail-Sammlung hinzugefügt: {FileName}",
        fileName);

    return Results.Ok(new
    {
        fileName,
        emailQueued = true
    });
=======
    var storage = app.Services.GetRequiredService<CaptureStorage>();
    await using var imageStream = image.OpenReadStream();
    var fileName = await storage.SaveAsync(imageStream, cancellationToken);

    return Results.Ok(new { fileName });
>>>>>>> c0cf6ffaac8778654cc91306d40c2c334c11e845
});

app.Run();
