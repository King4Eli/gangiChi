#!/usr/bin/env bash
set -euo pipefail

# MAUI on Linux can only build the Android target.
sudo dotnet workload install maui-android

# Download the Android SDK / platform packages the project needs.
dotnet build gangiChi/gangiChi.csproj -t:InstallAndroidDependencies \
  -f net8.0-android \
  -p:AndroidSdkDirectory="$ANDROID_HOME" \
  -p:JavaSdkDirectory="$JAVA_HOME" \
  -p:AcceptAndroidSDKLicenses=True

dotnet restore gangiChi/gangiChi.csproj
