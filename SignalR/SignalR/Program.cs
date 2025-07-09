
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using SignalR;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500", "https://localhost:7191")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "SignalRApp:";
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserCacheService, RedisUserCacheService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var userCacheService = scope.ServiceProvider.GetRequiredService<IUserCacheService>();
    await userCacheService.RemoveAllUserAsync();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<CallHub>("/callhub");
    endpoints.MapGet("/", () => "SignalR WebRTC API with Redis cache is running");
});

app.Run();



