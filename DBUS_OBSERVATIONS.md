While creating tests for this project, I had to setup dbus and secrets API services in a container, for isolated testing. Here are some facts I learned about these services:

- two buses: system and session (one session bus per user)
- buses are authenticated. Most secure and default option is by using unix sockets
- dbus-daemon is the dbus daemon. dbus-launch is a helper to launch it, related to X11 but apparently can be used in headless contexts too.
- When I make a secrets API request to dbus, it starts the gnome-keyring automatically, but its not unlocked
- we need to unlock (--unlock) *and* start (--start or --replace) the keyring demon to unlock. Only doing --unlock while a daemon it started is not enough, you also need to do --replace (in the same or another command, doesnt matter)
- Doing --unlock also starts the daemon (without --start) if not already started, and everything will work well.
