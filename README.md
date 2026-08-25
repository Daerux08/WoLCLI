# WoLCLI

A small, fast, cross-platform command-line tool to send Wake-on-LAN "magic packets" from your machine. Use WoLCLI to remotely wake computers on the same network or over routed/broadcast setups using MAC addresses, hostnames, or lists.

## Features
- Send Wake-on-LAN magic packets to a single host or many hosts.
- Accepts MAC addresses, hostnames, or an input file (CSV/JSON).
- Supports custom broadcast address, UDP port, and network interface selection.
- Works cross-platform (Windows, Linux, macOS) using .NET.
- Simple scripting-friendly output for automation and CI.

## Changes
- Made the app AOT, removing the need of having any .NET installation.


## Quick start
Prerequisites
- Network access to the target's broadcast domain (or configured routed relay)

Install (build from source)
```bash
git clone https://github.com/Daerux08/WoLCLI.git
cd WoLCLI
dotnet build -c Release
dotnet run -- --help
