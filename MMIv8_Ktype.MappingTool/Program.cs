using DevExpress.Blazor;
using MMIv8_Ktype.MappingTool.Components;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration() // Move to App settings
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:l}{NewLine}{Exception}")
    .WriteTo.File($"..\\MMIv8_Ktype.Core\\Logs\\Log.txt",
                  rollOnFileSizeLimit: true,
                  fileSizeLimitBytes: 1048576,
                  shared: true,
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:l}{NewLine}{Exception}",
                  restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

builder.Services.AddSingleton(Log.Logger);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDevExpressBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
