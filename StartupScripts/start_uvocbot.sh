#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

while IFS= read -r line; do
    export "$line"
done < $DIR/uvocbot.env

$DIR/UVOCBot
