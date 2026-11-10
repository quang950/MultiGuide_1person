FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MultiGuide.sln ./
COPY src/MultiGuide.Domain/MultiGuide.Domain.csproj     src/MultiGuide.Domain/
COPY src/MultiGuide.Business/MultiGuide.Business.csproj src/MultiGuide.Business/
COPY src/MultiGuide.Data/MultiGuide.Data.csproj         src/MultiGuide.Data/
COPY src/MultiGuide.Api/MultiGuide.Api.csproj           src/MultiGuide.Api/
RUN dotnet restore src/MultiGuide.Api/MultiGuide.Api.csproj
COPY src/ src/
RUN dotnet publish src/MultiGuide.Api/MultiGuide.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Cài đặt Python 3 và gói edge-tts cho tính năng sinh giọng đọc Neural trên Render Cloud
RUN apt-get update && apt-get install -y --no-install-recommends python3 python3-pip python3-venv && rm -rf /var/lib/apt/lists/*
RUN python3 -m venv /opt/venv && /opt/venv/bin/pip install --no-cache-dir edge-tts
ENV PATH="/opt/venv/bin:$PATH"

WORKDIR /app
COPY --from=build /app .
COPY scripts/ /app/scripts/
# Render cấp biến PORT; mặc định 10000
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet MultiGuide.Api.dll"]
