#! /bin/bash

./bin/Debug/net6.0/BuildVersion.exe \
        --namespace org.herbal3d.buildVersion \
        --version $(cat VERSION) \
        --incrementBuild \
        --writeAppVersion VERSION
