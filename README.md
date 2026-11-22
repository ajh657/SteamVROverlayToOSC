# SteamVROverlayToOSC

A Windows-only .NET MAUI application that provides a background worker and a
separate OSC UDP client.

## Build

Requires the .NET 8 MAUI workload on Windows:

```powershell
dotnet workload install maui-windows
dotnet build .\SteamVROverlayToOSC.sln
```