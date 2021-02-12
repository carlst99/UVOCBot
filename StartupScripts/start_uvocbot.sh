#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

export "DOTNET_ROOT=path/to/your/dotnet/installation"
export "DOTNET_ENVIRONMENT=Release"

$DIR/UVOCBot
