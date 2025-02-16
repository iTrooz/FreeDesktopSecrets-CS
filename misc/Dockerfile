# This image contains a dbus session bus exposed over TCP and a gnome-keyring daemon.
# The goal is to be able to use the Freedesktop secret service API with this.
FROM ubuntu:latest

# Install necessary packages
RUN --mount=type=cache,target=/var/cache/apt \
    apt-get update && apt-get install -y dbus gnome-keyring libsecret-tools

ENV DBUS_SESSION_BUS_ADDRESS=tcp:host=0.0.0.0,port=7834
ENV UNLOCK_PASSWORD=unlockPassword

COPY session.conf /etc/dbus-1/session.conf

# Set up dbus. Idk why it has to be done in one step, but it does
RUN <<EOF
    set -xe

    # Setup dbus
    dbus-daemon --config-file=/etc/dbus-1/session.conf --print-address --address=${DBUS_SESSION_BUS_ADDRESS} --fork
    sleep 1

    # Test
    dbus-send --session --dest=org.freedesktop.DBus --type=method_call --print-reply / org.freedesktop.DBus.ListNames

    # Setup gnome-keyring
    echo $UNLOCK_PASSWORD | gnome-keyring-daemon --unlock # will start too
    sleep 1

    # Test
    echo "password1" | secret-tool store --label=label1 attrkey1 attrvalue1
    secret-tool clear attrkey1 attrvalue1
EOF

EXPOSE 7834/tcp
COPY start.sh .
ENTRYPOINT ["./start.sh"]
