using Docker.DotNet;
using DockWatch.Model;

namespace DockWatch.Services;

public class DockerService
{    
    private readonly IDockerClient _client;
    public DockerService(IDockerClient client) => _client = client;

    public async Task<IReadOnlyList<ContainerSummaryView>> ListAsync(CancellationToken ct = default)
    {
        var containers = await _client.Containers.ListContainersAsync(
            new Docker.DotNet.Models.ContainersListParameters { All = true }, ct);

        return containers.Select(c => new ContainerSummaryView
        {
            Id = c.ID,
            Name = c.Names?.FirstOrDefault()?.TrimStart('/'),
            Image = c.Image,
            State = c.State,
            Status = c.Status,
            Ports = string.Join(", ",
                c.Ports?.Select(p => $"{p.PublicPort}->{p.PrivatePort}/{p.Type}") ?? Array.Empty<string>())
        }).ToList();
    }

    public Task<bool> StartAsync(string id, CancellationToken ct = default) =>
        _client.Containers.StartContainerAsync(
            id, new Docker.DotNet.Models.ContainerStartParameters(), ct);

    public async Task<bool> StopAsync(string id, CancellationToken ct = default)
    {
        await _client.Containers.StopContainerAsync(
            id, new Docker.DotNet.Models.ContainerStopParameters(), ct);
        return true; // no exception == success
    }

    public async Task<bool> RestartAsync(string id, CancellationToken ct = default)
    {
        await _client.Containers.RestartContainerAsync(
            id, new Docker.DotNet.Models.ContainerRestartParameters(), ct);
        return true; // no exception == success
    }
}