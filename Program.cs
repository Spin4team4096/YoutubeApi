using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Endpoint for VIDEO-ONLY MP4
app.MapGet("/getVideo", async (HttpContext context, [FromQuery] string videoId) =>
{
    var youtube = new YoutubeClient();
    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
    var streamInfo = streamManifest.GetVideoOnlyStreams().GetWithHighestBitrate();

    if (streamInfo == null)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Error: No valid video stream found.");
        return;
    }

    using var httpClient = new HttpClient();
    var stream = await httpClient.GetStreamAsync(streamInfo.Url);

    context.Response.ContentType = "video/mp4";
    context.Response.Headers["Content-Disposition"] = "inline; filename=\"video.mp4\"";
    await stream.CopyToAsync(context.Response.Body);
});

// Endpoint for AUDIO-ONLY M4A
app.MapGet("/getAudio", async (HttpContext context, [FromQuery] string videoId) =>
{
    var youtube = new YoutubeClient();
    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
    var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

    if (streamInfo == null)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Error: No valid audio stream found.");
        return;
    }

    using var httpClient = new HttpClient();
    var stream = await httpClient.GetStreamAsync(streamInfo.Url);

    context.Response.ContentType = "audio/mp4"; // M4A format
    context.Response.Headers["Content-Disposition"] = "inline; filename=\"audio.m4a\"";
    await stream.CopyToAsync(context.Response.Body);
});

app.Run();