#!/bin/bash
clear
echo "installing fake with nuget"
mono ".nuget/NuGet.exe" "Install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion"


