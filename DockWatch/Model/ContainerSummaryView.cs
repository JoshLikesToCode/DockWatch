namespace DockWatch.Model;

public record ContainerSummaryView
{
    public string Id { get; init; } = "";
    public string? Name { get; init; }
    public string Image { get; init; } = "";
    public string State { get; init; } = "";
    public string? Status { get; init; }
    public string? Ports { get; init; }
}