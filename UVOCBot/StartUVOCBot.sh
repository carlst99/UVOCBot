#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

export DOTNET_ROOT="path/to/your/dotnet/installation"
export UVOCBOT_TWITTERAPI_KEY="Your Twitter API key"
export UVOCBOT_BOT_TOKEN="Your Discord application Bot Token"
export DOTNET_ENVIRONMENT="Release"
export UVOCBOT_TWITTERAPI_SECRET="Your Twitter API secret"
export UVOCBOT_TWITTERAPI_BEARER_TOKEN="Your Twitter API bearer token"
export UVOCBOT_API_ENDPOINT="http://localhost:52728/api"

$DIR/UVOCBot
