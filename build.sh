#!/bin/bash

target="Default"
if [ -n "$1" ]; then target=$1; fi
    
echo Building $target
chmod +x ./packages/FAKE/tools/FAKE.exe
./packages/FAKE/tools/FAKE.exe ./Builder/Build.fsx $target
