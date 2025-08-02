#!/bin/bash

docker builder prune -f  # clear old cache
docker build -t telecasino-kenogame .
docker run --rm -p 8080:8080 -e DOTNET_ENVIRONMENT=Development telecasino-kenogame

