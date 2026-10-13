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
WORKDIR /app
COPY --from=build /app .
# Render cấp biến PORT; mặc định 10000
CMD ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-10000} dotnet MultiGuide.Api.dll"]