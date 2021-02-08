#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

export "DOTNET_ROOT=path/to/your/dotnet/installation"
export "ASPNETCORE_ENVIRONMENT=Release"
export "ASPNETCORE_URLS=http://localhost:42718"

$DIR/UVOCBot.Api
