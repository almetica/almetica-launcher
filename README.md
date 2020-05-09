# Almetica-launcher

[![Gitter](https://badges.gitter.im/almetica-server/community.svg)](https://gitter.im/almetica-server/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

This is a custom client launcher for the MMORPG TERA that interfaces directly with the client.
It's re-implementing the protocol used by the Tl.exe and the EME launcher.

It's main purpose serves as the launcher when using the custom server implementation Almetica.

## Requirements

Visual Studio 2019 Community Edition+. Written in .NET Core C#.

## Building

This is a standard .NET Core project. The single file executable creation is enabled by default, so
you should use a publish executable. The 32bit client of TERA needs a 32bit launcher.
The 64bit client of TERA will need a 64bit launcher later on.

The builds that we distribute are self contained.

## Configuration

Configure the launcher with the help of the provided configuration file
(Configuration.json). 

You need to set at least the address of the server and may optionally set the
standard username and password to use (for development).

Possible values for the language are:
 - EUR = English with the EU client
 - USA = English with the US client
 - FRA = French with the EU client
 - GER = German with the EU client

## Running

Copy the following file into the client folder (the folder with the Binares,
Engine and S1Game folder as well as the version.txt/ini file):

 - AlmeticaLauncher.exe

The file is including all dependencies.

## Protocol Description

The game needs to be launched by the launcher as a child process. The launcher needs to have a
window with the window title "LAUNCHER_WINDOW" and window class "LAUNCHER_CLASS". The client
checks these conditions and won't start otherwise.

The launcher communicates with the game using Windows Messages (```SendMessage``` / ```WNDPROC```).
The data is passed with the help of the WM_COPYDATA (0x4A) message. The payload is referenced with
the ```lpData``` parameter as a ```COPYDATASTRUCT```. The ```dwData``` field contains the event ID
used by the game (game event).

A game event equal or over 1000 signals a special event inside the game that is mostly informational
and the launcher is not required to handle them in any special way (except sending an 0x1 reply).

Game events for example are:
 - 1000: Game started
 - 1001: Game loaded
 - 1003: Server selection
 - 1004: Character selection
 - 1020: Game terminates
 - ...

The game events lower than 10 are required to be handled by the launcher stalling the request and
NOT sending a reply of 0x1 right away. While stalling the request, the launcher is required to send
the expected data using ```SendMessage```. The game expects to find the data in it's message queue
when returning from it's ```SendMessage``` function (it's code is synchronous). Events that need
to be handled are:

### Event 1: Account Name

The launcher needs to reply with a ```WM_COPY``` message with it's dwData field set with 2.
The payload (```lpData```) needs to be an UTF-16 encoded string without null termination containing
the account name.

### Event 3: Token

The launcher needs to reply with a ```WM_COPY``` message with it's dwData field set with 4.
The payload (```lpData```) needs to be an byte array containing the token.

### Event 5: Server List

The launcher needs to reply with a ```WM_COPY``` message with it's dwData field set with 6.
The payload (```lpData```) needs to be a protocol buffer (version 2) containins a list with the
servers that should be shown in the server selection screen. For a field description have a look
at the protocol buffer file provided with this repository. You can use protocol buffer version 3
if you make sure that fields of 0 value are serialized. The developers of protocol buffer broke
backward compatability with legacy applications by not supporting required fields anymore. You can
change the generated protocol buffer code manually to send the 0 value fields anyhow.

## Secret Named Pipe

The TERA client tries to connect to a named pipe when starting. The launcher can create the TERA
client in suspended mode, create the expected named pipe and then resume the main thread of the
TERA client to get it connected to the pipe. This is needed, since the pipe name contains the PID
of the TERA client. The pipe name is: ```\\.\pipe\$PIDcout```.

The exact function of the pipe is yet unknown

## License

Licensed under GPL version 3.

The GNU General Public License is often called the GNU GPL for short; it is used
by most GNU programs, and by more than half of all free software packages.
The latest version is version 3. 
