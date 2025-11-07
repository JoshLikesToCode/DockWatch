using System.Runtime.InteropServices;
using Docker.DotNet;
using DockWatch.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor components with server interactivity
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Docker client registration
builder.Services.AddSingleton<IDockerClient>(_ =>
{
    var dockerUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new Uri("npipe://./pipe/docker_engine")
        : new Uri("unix:///var/run/docker.sock");
    return new DockerClientConfiguration(dockerUri).CreateClient();
});

// Docker service
builder.Services.AddSingleton<DockerService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Map Razor components
app.MapRazorComponents<DockWatch.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/", () => Results.Redirect("/containers"));

app.Run();