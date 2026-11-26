ARG AGENT=claude
FROM docker.io/sbx/${AGENT}:latest

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libfontconfig1 \
        libicu76 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=mcr.microsoft.com/dotnet/sdk:10.0 /usr/share/dotnet /usr/share/dotnet

RUN ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet

USER agent

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    UseArtifactsOutput=true \
    ArtifactsPath=/home/agent/artifacts
