using MediaServer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 20L * 1024 * 1024 * 1024;
});

builder.Services.AddControllers();

builder.Services.AddHttpClient<D1Service>(client =>
{
    client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<VideoService>();

var app = builder.Build();

app.MapControllers();


// آدرس اصلی سرویس
app.MapGet("/", () => "MediaServer.Api is running!");

// بررسی سلامت سرویس
app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        Status = "ok",
        Service = "MediaServer.Api"
    });
});

app.Run();