using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Return VIDEO MP4 with optional quality parameter
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

    // If quality doesn't exist, try 480p
    if (streamInfo == null)
    {
        streamInfo = streamManifest.GetVideoOnlyStreams()
            .FirstOrDefault(s => s.VideoQuality.Label == "480p");
    }

    // If 480p is also nonexistent, pick the highest quality that does exist
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

// Return AUDIO M4A
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