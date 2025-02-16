FROM ubuntu:latest

# Install necessary packages
RUN --mount=type=cache,target=/var/cache/apt \
    apt-get update && apt-get install -y dbus gnome-keyring libsecret-tools

# Set up dbus. Idk why it has to be done in one step, but it does
RUN <<EOF
    set -xe
    mkdir -p /tmp/sock
    dbus-daemon --print-address --address=unix:path=/tmp/sock/dbus --session --fork
    export DBUS_SESSION_BUS_ADDRESS=unix:path=/tmp/sock/dbus
    dbus-send --session --dest=org.freedesktop.DBus --type=method_call --print-reply / org.freedesktop.DBus.ListNames
    printf '\n' | gnome-keyring-daemon --unlock
    printf '\n' | gnome-keyring-daemon --start

    # Test
    echo "password1" | secret-tool store --label=label1 attrkey1 attrvalue1
    secret-tool clear attrkey1 attrvalue1
EOF

ENV DBUS_SESSION_BUS_ADDRESS=unix:path=/tmp/sock/dbus


CMD ["dbus-daemon", "--address=unix:path=/tmp/sock/dbus", "--session"]
