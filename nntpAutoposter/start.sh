#!/bin/bash

cd ${0%/*}

if ! screen -ls | grep -q "nntpAutoPoster"; then
screen -S nntpAutoPoster -d -m -L mono nntpAutoposter.exe
echo 'NNTP auto poster started.'
fi