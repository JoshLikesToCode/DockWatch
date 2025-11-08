using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DockWatch.Services;
using DockWatch.Model;
using FluentAssertions;
using Moq;
using Xunit;
using System.Collections.Generic;

namespace DockWatch.Tests;

public class DockerServiceTests
{
    private static (DockerService svc, Mock<IDockerClient> clientMock, Mock<IContainerOperations> containersMock) CreateService()
    {
        var containersMock = new Mock<IContainerOperations>(MockBehavior.Strict);
        var clientMock = new Mock<IDockerClient>(MockBehavior.Strict);
        clientMock.SetupGet(c => c.Containers).Returns(containersMock.Object);
        var svc = new DockerService(clientMock.Object);
        return (svc, clientMock, containersMock);
    }

    [Fact]
    public async Task ListAsync_Maps_Fields()
    {
        // Arrange
        var (svc, _, containersMock) = CreateService();
        var list = new List<ContainerListResponse>
        {
            new ContainerListResponse
            {
                ID = "abcdef1234567890",
                Names = new[] { "/web" },
                Image = "nginx:latest",
                State = "running",
                Status = "Up 2 minutes",
                Ports = new[] { new Port { PrivatePort = 80, PublicPort = 8080, Type = "tcp" } }
            }
        };
        containersMock
            .Setup(m => m.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list as IList<ContainerListResponse>);

        // Act
        var result = await svc.ListAsync();

        // Assert
        result.Should().HaveCount(1);
        var c = result[0];
        c.Id.Should().Be("abcdef1234567890");
        c.Name.Should().Be("web");
        c.Image.Should().Be("nginx:latest");
        c.State.Should().Be("running");
        c.Status.Should().Be("Up 2 minutes");
        c.Ports.Should().Be("8080->80/tcp");
    }

    [Fact]
    public async Task Start_Stop_Restart_Return_True_And_Call_Client()
    {
        // Arrange
        var (svc, _, containersMock) = CreateService();
        containersMock
            .Setup(m => m.StartContainerAsync("id1", It.IsAny<ContainerStartParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Verifiable();
        containersMock
            .Setup(m => m.StopContainerAsync("id1", It.IsAny<ContainerStopParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Verifiable();
        containersMock
            .Setup(m => m.RestartContainerAsync("id1", It.IsAny<ContainerRestartParameters>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var started = await svc.StartAsync("id1");
        var stopped = await svc.StopAsync("id1");
        var restarted = await svc.RestartAsync("id1");

        // Assert
        started.Should().BeTrue();
        stopped.Should().BeTrue();
        restarted.Should().BeTrue();
        containersMock.VerifyAll();
    }

    [Fact]
    public async Task GetSelfContainerIdAsync_Matches_By_Hostname_StartsWith()
    {
        // Arrange
        var (svc, _, containersMock) = CreateService();
        var host = Environment.MachineName; // whatever it is, ensure one entry starts with it
        var list = new List<ContainerListResponse>
        {
            new ContainerListResponse { ID = host + "1234567890", Names = new []{"/not-self"} },
            new ContainerListResponse { ID = "otherid", Names = new []{"/web"} },
        };
        containersMock
            .Setup(m => m.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list as IList<ContainerListResponse>);

        // Act
        var id = await svc.GetSelfContainerIdAsync();

        // Assert
        id.Should().NotBeNull();
        id!.StartsWith(host, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }
}
