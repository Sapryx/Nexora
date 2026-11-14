FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        jq \
        libfontconfig1 \
        python3 \
    && rm -rf /var/lib/apt/lists/*

RUN useradd --create-home --shell /bin/bash agent
USER agent
WORKDIR /home/agent

RUN curl -fsSL https://claude.ai/install.sh | bash \
    && mkdir -p .claude .nuget/packages artifacts \
    && git config --global --add safe.directory '*' \
    && git config --global core.autocrlf input

ENV PATH=/home/agent/.local/bin:$PATH \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    UseArtifactsOutput=true \
    ArtifactsPath=/home/agent/artifacts

WORKDIR /work
