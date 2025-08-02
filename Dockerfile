# ---------- STAGE 1: Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj & restore dependencies
COPY *.csproj ./
RUN dotnet restore

# copy the rest of the source
COPY . .

# ✅ Explicitly build for Linux
RUN dotnet publish -c Release -r linux-x64 --self-contained false -o /app/publish

# ---------- STAGE 2: Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install SkiaSharp dependencies
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
    libfribidi0 \
    libxcb1 \
    libc6 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    && rm -rf /var/lib/apt/lists/*

# Copy app files
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TeleCasino.KenoGameService.dll"]
