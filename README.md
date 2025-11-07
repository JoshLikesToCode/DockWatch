The desired outcome of this project is a Blazor Server app that:

- Lists local Docker containers (name, image, status, ports)

- Start/Stop/Restart a container from the UI

- Runs locally or in Docker (by mounting the Docker socket

To use DockWatch via Docker, use your command line to run the following
command in the same directory as this projects Dockerfile:

docker run --name dockwatch \
  -p 8080:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  dockwatch
