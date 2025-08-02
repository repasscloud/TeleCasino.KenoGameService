# ---------- STAGE 1: Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . .

# ✅ Publish inside Linux environment with Linux RID
RUN dotnet publish -c Release -r linux-x64 --self-contained false /p:PublishReadyToRun=true -o /app/publish

# ---------- STAGE 2: Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

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

COPY --from=build /app/publish .

RUN mkdir -p /shared/Keno/images
COPY Assets/KenoImages/* /shared/Keno/images/

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TeleCasino.KenoGameService.dll"]
