using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Endpoint for VIDEO-ONLY MP4 with optional quality parameter
app.MapGet("/getVideo", async (HttpContext context, [FromQuery] string videoId, [FromQuery] string? quality) =>
{
    var youtube = new YoutubeClient();
    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);

    // If a specific quality is requested, find the stream with that quality
    IStreamInfo? streamInfo = null;

    if (!string.IsNullOrEmpty(quality))
    {
        // Try to find the video stream matching the requested quality
        var qualityValue = quality.ToLower();
        streamInfo = streamManifest.GetVideoOnlyStreams()
            .FirstOrDefault(s => s.VideoQuality.ToString().ToLower() == qualityValue);
    }

    // If no specific quality is requested or the requested quality isn't found, pick the highest bitrate
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

    return Results.Ok(); // Return success status
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

    return Results.Ok(); // Return success status
});

app.Run();