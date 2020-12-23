#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

export DOTNET_ROOT="path/to/your/dotnet/installation"
export ASPNETCORE_ENVIRONMENT="Release"
export ASPNETCORE_URLS="http://localhost:52728"
export UVOCBOTAPI_DB_PASSWD="The password for your database user"
export UVOCBOTAPI_DB_USER="The user for your database"
export UVOCBOTAPI_DB_NAME="The name of your database"
export UVOCBOTAPI_DB_SERVER="The address of your database server"

$DIR/UVOCBot.Api
