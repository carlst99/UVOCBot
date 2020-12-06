#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

export DOTNET_ROOT="path/to/your/dotnet/installation"
export UVOCBOT_DB_PASSWD="The password for your database user"
export UVOCBOT_TWITTERAPI_KEY="Your Twitter API key"
export UVOCBOT_DB_USER="The user for your database"
export UVOCBOT_BOT_TOKEN="Your Discord application Bot Token"
export DOTNET_ENVIRONMENT="Release"
export UVOCBOT_TWITTERAPI_SECRET="Your Twitter API secret"
export UVOCBOT_DB_NAME="The name of your database"
export UVOCBOT_DB_SERVER="The address of your database server"
export UVOCBOT_TWITTERAPI_BEARER_TOKEN="Your Twitter API bearer token"

$DIR/UVOCBot
