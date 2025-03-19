using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors.Infrastructure;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

var builder = WebApplication.CreateBuilder(args);

// Allow CORS for all origins (You can specify specific origins if needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // Allow any origin to access the API
              .AllowAnyMethod()  // Allow all HTTP methods (GET, POST, etc.)
              .AllowAnyHeader(); // Allow all headers (including Content-Type)
    });
});

var app = builder.Build();

// Use CORS policy globally
app.UseCors("AllowAll");

// Endpoint for VIDEO-ONLY MP4 with optional quality parameter
app.MapGet("/getVideo", async (HttpContext context, [FromQuery] string videoId, [FromQuery] string? quality) =>
{
    var youtube = new YoutubeClient();
    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);

    IStreamInfo? streamInfo = null;

    if (!string.IsNullOrEmpty(quality))
    {
        // Try to find the video stream matching the requested quality
        var qualityValue = quality.ToLower();
        streamInfo = streamManifest.GetVideoOnlyStreams()
            .FirstOrDefault(s => s.VideoQuality.ToString().ToLower() == qualityValue);
    }

    // If no specific quality is requested or the requested quality isn't found, try 480p
    if (streamInfo == null)
    {
        streamInfo = streamManifest.GetVideoOnlyStreams()
            .FirstOrDefault(s => s.VideoQuality.Label == "480p");
    }

    // If 480p is also unavailable, pick the highest bitrate
    if (streamInfo == null)
    {
        streamInfo = streamManifest.GetVideoOnlyStreams().GetWithHighestBitrate();
    }

    if (streamInfo == null)
    {
        return Results.NotFound("Error: No valid video stream found.");
    }

    using var httpClient = new HttpClient();
    var stream = await httpClient.GetStreamAsync(streamInfo.Url);

    context.Response.ContentType = "video/mp4";
    context.Response.Headers["Content-Disposition"] = "inline; filename=\"video.mp4\"";
    await stream.CopyToAsync(context.Response.Body);

    return Results.Ok();
});

// Endpoint for AUDIO-ONLY M4A (no change here)
app.MapGet("/getAudio", async (HttpContext context, [FromQuery] string videoId) =>
{
    var youtube = new YoutubeClient();
    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
    var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

    if (streamInfo == null)
    {
        return Results.NotFound("Error: No valid audio stream found.");
    }

    using var httpClient = new HttpClient();
    var stream = await httpClient.GetStreamAsync(streamInfo.Url);

    context.Response.ContentType = "audio/mp4"; // M4A format
    context.Response.Headers["Content-Disposition"] = "inline; filename=\"audio.m4a\"";
    await stream.CopyToAsync(context.Response.Body);

    return Results.Ok();
});

app.Run();
