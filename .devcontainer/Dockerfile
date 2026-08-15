FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm

# The base image ships a stale /etc/apt/sources.list.d/yarn.list whose GPG
# key (62D54FD4003F6525) no longer verifies, so any `apt-get update` run
# after this point fails with "NO_PUBKEY". That breaks the docker-in-docker
# feature's own apt-get update during container creation. Node/Yarn tooling
# comes from the node feature in devcontainer.json instead, so this repo
# isn't needed — drop it before Dev Container features run their installs.
# See: https://github.com/devcontainers/images/issues/1797
RUN rm -f /etc/apt/sources.list.d/yarn.list
