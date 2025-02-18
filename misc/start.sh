#!/bin/sh

echo "Starting dbus daemon"

# Can't use --fork, or we can't use `wait` (process is not a child of this shell)
dbus-daemon --address=$DBUS_SESSION_BUS_ADDRESS --session&
sleep 1

echo "Starting the gnome keyring daemon"
# It seems dbus-daemon starts/replaces the keyring daemon on startup, but without unlocking it.
# We can't simply unlock it, we have to *replace it*.
echo $UNLOCK_PASSWORD | gnome-keyring-daemon --unlock --replace
sleep 1

# Test one last time
echo "password1" | secret-tool store --label=label1 attrkey1 attrvalue1
secret-tool clear attrkey1 attrvalue1

wait $(pgrep dbus-daemon)
