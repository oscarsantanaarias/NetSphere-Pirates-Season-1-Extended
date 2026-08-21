# S4 League Season 1 Server Emulator
Based on WTFBlub's original version of his season 1 Patch 26 Server Emulator: https://github.com/wtfblub/NetspherePirates

This is an extended, bug fixed &amp; improved upon version of Wtfblub's Netsphere Pirates S4 League season 1 server emulator I've been working on.

Special thanks for VV, JuanCMC, Santana5322 & Wizzardo for helping contribute to the codebase.

Here's a quick overview of what's working, for a more detailed breakdown please refer to the issues tab.
* Deathmatch, TouchDown, BattleRoyale & Chaser game modes are all working
* General gameplay during matches working fine
* Shop, inventory & character system
* Room management & match settings working fine
* Working P2P implemenation


Tutorials, Documentation & Resources are here within the project's wiki:
[Netsphere Pirates S1 Extended Wiki](https://github.com/shanzenos/NetSphere-Pirates-Season-1-Extended/wiki)

Requirements: 
* [MySQL](https://www.mysql.com/) / MariaDB or SQLite
* [C++ 2015 Redist](https://www.microsoft.com/en-us/download/details.aspx?id=48145) for the game client
* [.NET SDK 10](https://dotnet.microsoft.com/download) to build (projects multi-target `net48` and `net10.0`)

## Building

All projects are SDK-style and multi-target `net48` (classic Windows build) and `net10.0` (cross-platform).
Open `NetspherePirates.sln` in Visual Studio 2022+ or use the CLI:

```
dotnet build src/Auth/Auth.csproj
dotnet build src/Game/Game.csproj
```

Outputs land in `bin/<Configuration>/net48/` and `bin/<Configuration>/net10.0/`.

## Running on Linux

Publish self-contained binaries (no runtime install needed on the server):

```
dotnet publish src/Auth/Auth.csproj -f net10.0 -r linux-x64 --self-contained -c Release -o publish/Auth
dotnet publish src/Game/Game.csproj -f net10.0 -r linux-x64 --self-contained -c Release -o publish/Game
```

Upload both folders, then:

```
chmod +x Auth/Auth Game/Game
cd Auth && ./Auth
cd Game && ./Game
```

`auth.hjson` / `game.hjson` sit next to the binaries. The MySQL driver is MySqlConnector, which works with MariaDB 10.6+ (`utf8mb3`) out of the box.

Optional:
* [AIO repack](https://github.com/abbodi1406/vcredist)
* [Game Client (Patch 26(EU v1120)](https://archive.org/download/s4lgameclientarchives/S4%20League%20Game%20Client%20Archive/%28MAIN%29%20Season%201%20-%20Patch%2026%28EU%20v1120%29/FumbiClient.7z)
