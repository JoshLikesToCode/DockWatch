using System.Linq;
using System.Threading.Tasks;
using Bunit;
using DockWatch.Components.Pages;
using DockWatch.Model;
using DockWatch.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;

namespace DockWatch.Tests;

public class ContainersComponentTests : TestContext
{
    private static (Mock<IDockerClient> clientMock, Mock<IContainerOperations> containersMock, ContainerSummaryView[] initial) ConfigureServices(TestContext ctx)
    {
        var containersMock = new Mock<IContainerOperations>(MockBehavior.Strict);
        var clientMock = new Mock<IDockerClient>(MockBehavior.Strict);
        clientMock.SetupGet(c => c.Containers).Returns(containersMock.Object);

        var host = Environment.MachineName;
        var selfId = host + "abcdef"; // ensures GetSelfContainerIdAsync matches by StartsWith(host)
        var initial = new[]
        {
            new ContainerSummaryView{ Id = selfId, Name = "dockwatch", Image = "app:latest", State = "running", Status = "Up", Ports = ""},
            new ContainerSummaryView{ Id = "abcdef1234567890", Name = "web", Image = "nginx:latest", State = "exited", Status = "Exited", Ports = "8080->80/tcp"},
        };

        // Docker API returned objects
        var apiList = initial.Select(v => new ContainerListResponse
        {
            ID = v.Id,
            Names = new[] { "/" + v.Name },
            Image = v.Image,
            State = v.State,
            Status = v.Status,
            Ports = string.IsNullOrEmpty(v.Ports) ? Array.Empty<Port>() : new[] { new Port { PrivatePort = 80, PublicPort = 8080, Type = "tcp" } }
        }).ToList();

        // Sequence of calls:
        // 1) GetSelfContainerIdAsync -> ListContainersAsync
        // 2) Load/ListAsync -> ListContainersAsync
        containersMock.SetupSequence(m => m.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiList)
            .ReturnsAsync(apiList)
            .ReturnsAsync(apiList);

        containersMock
            .Setup(m => m.StartContainerAsync("abcdef1234567890", It.IsAny<ContainerStartParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        containersMock
            .Setup(m => m.StopContainerAsync("abcdef1234567890", It.IsAny<ContainerStopParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        containersMock
            .Setup(m => m.RestartContainerAsync("abcdef1234567890", It.IsAny<ContainerRestartParameters>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Register real DockerService instance backed by mocked IDockerClient
        ctx.Services.AddSingleton(new DockerService(clientMock.Object));
        return (clientMock, containersMock, initial);
    }

    [Fact]
    public void Render_Shows_Rows_And_Disables_Self()
    {
        var (clientMock, containersMock, initial) = ConfigureServices(this);

        var cut = RenderComponent<Containers>();

        // bUnit may complete async init quickly; assert on final rendered state
        cut.WaitForAssertion(() =>
        {
            // Two rows present (one for each container)
            var rows = cut.FindAll("tbody tr");
            rows.Should().HaveCount(2);
            rows[0].InnerHtml.Should().Contain(initial[0].Name!);
            rows[1].InnerHtml.Should().Contain(initial[1].Name!);
            // Self row should have disabled buttons
            var selfButtons = rows[0].QuerySelectorAll("button");
            selfButtons.Length.Should().Be(3);
            selfButtons.All(b => b.HasAttribute("disabled")).Should().BeTrue();
        });

        containersMock.Verify(m => m.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public void Click_Start_Invokes_Service_And_Refreshes()
    {
        var (clientMock, containersMock, initial) = ConfigureServices(this);
        var cut = RenderComponent<Containers>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain(initial[1].Name!));

        // Click Start on the non-self row (second table row)
        var rows = cut.FindAll("tbody tr");
        rows.Should().HaveCount(2);
        var nonSelfButtons = rows[1].QuerySelectorAll("button");
        nonSelfButtons.Length.Should().Be(3);
        var start = nonSelfButtons[0];
        start.HasAttribute("disabled").Should().BeFalse();
        start.Click();

        // After click, the Docker client should receive Start and list should reload
        cut.WaitForAssertion(() =>
        {
            containersMock.Verify(m => m.StartContainerAsync("abcdef1234567890", It.IsAny<ContainerStartParameters>(), It.IsAny<CancellationToken>()), Times.Once());
            containersMock.Verify(m => m.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
        });
    }
}
