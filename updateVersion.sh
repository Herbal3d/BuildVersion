#! /bin/bash

./bin/Debug/net6.0/BuildVersion.exe \
        --verbose \
        --namespace org.herbal3d.buildVersion \
        --version $(cat VERSION) \
        --incrementBuild \
        --writeAppVersion VERSION
