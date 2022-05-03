#! /bin/bash

./bin/Debug/BuildVersion.exe \
        --verbose \
        --namespace org.herbal3d.buildVersion \
        --version $(cat VERSION) \
        --incrementBuild \
        --writeAppVersion VERSION \
        --assemblyInfoFile Properties/AssemblyInfo.cs
