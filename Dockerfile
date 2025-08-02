# ---------- STAGE 1: Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj & restore dependencies (cache layer)
COPY *.csproj ./
RUN dotnet restore

# copy the rest and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ---------- STAGE 2: Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install ffmpeg and SkiaSharp dependencies
RUN apt update && apt -y install --no-install-recommends \
    ffmpeg \
    libfontconfig1 \
    libfreetype6 \
    libpng16-16 \
    libjpeg62-turbo \
    libgif7 \
    libwebp7 \
    libharfbuzz0b \
    libicu72 \
    libgl1 \
    && rm -rf /var/lib/apt/lists/*

# copy published output
COPY --from=build /app/publish .

# copy keno images to shared volume path
RUN mkdir -p /shared/Keno/images
COPY Assets/KenoImages/* /shared/Keno/images/

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TeleCasino.KenoGameService.dll"]