using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Net;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

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

    // Get cookies from environment variables
    var cookies = Environment.GetEnvironmentVariable("YOUTUBE_COOKIES");
    if (string.IsNullOrEmpty(cookies))
    {
        return Results.BadRequest("Error: No YouTube cookies found.");
    }

    // Prepare HttpClient with cookies
    var cookieContainer = new CookieContainer();
    foreach (var cookie in cookies.Split(';'))
    {
        var parts = cookie.Split('=');
        if (parts.Length == 2)
        {
            cookieContainer.Add(new Uri("https://www.youtube.com"), new Cookie(parts[0], parts[1]));
        }
    }

    var handler = new HttpClientHandler
    {
        CookieContainer = cookieContainer
    };

    using var httpClient = new HttpClient(handler);
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

    var cookies = Environment.GetEnvironmentVariable("YOUTUBE_COOKIES");
    if (string.IsNullOrEmpty(cookies))
    {
        return Results.BadRequest("Error: No YouTube cookies found.");
    }

    // Prepare HttpClient with cookies
    var cookieContainer = new CookieContainer();
    foreach (var cookie in cookies.Split(';'))
    {
        var parts = cookie.Split('=');
        if (parts.Length == 2)
        {
            cookieContainer.Add(new Uri("https://www.youtube.com"), new Cookie(parts[0], parts[1]));
        }
    }

    var handler = new HttpClientHandler
    {
        CookieContainer = cookieContainer
    };

    using var httpClient = new HttpClient(handler);
    var stream = await httpClient.GetStreamAsync(streamInfo.Url);

    context.Response.ContentType = "audio/mp4"; // M4A format
    context.Response.Headers["Content-Disposition"] = "inline; filename=\"audio.m4a\"";
    await stream.CopyToAsync(context.Response.Body);

    return Results.Ok();
});

app.Run();
