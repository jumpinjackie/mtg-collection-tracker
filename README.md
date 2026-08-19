# mtg-collection-tracker

[![Actions Status](https://github.com/jumpinjackie/mtg-collection-tracker/workflows/.NET/badge.svg)](https://github.com/jumpinjackie/mtg-collection-tracker/actions)

`mtg-collection-tracker` is a multi-platform application for managing for your Magic: The Gathering card collection

# How to build

 1. Clone this repo
 2. `dotnet restore`
 3. `dotnet build`
 4. `cd src/MtgCollectionTracker.Desktop`
 5. `dotnet run`

# AI Inspection via MCP (XamlMcp)

The desktop app ships with the `XamlMcp.Avalonia` agent attached. The agent is
inert unless the app is launched with the `XAML_MCP=1` environment variable, so
nothing listens by default.

## 1. Install the MCP server

```bash
dotnet tool install --global XamlMcp.Server --version 1.0.0-preview.3
```

## 2. Launch the app with the agent enabled

```bash
XAML_MCP=1 dotnet run --project src/MtgCollectionTracker.Desktop
```

(PowerShell: `$env:XAML_MCP="1"; dotnet run --project src/MtgCollectionTracker.Desktop`)

## 3. Register the server with your MCP client

```bash
claude mcp add --scope user xamlmcp -- xamlmcp
```

Then ask the client to `list-apps` → `attach(instanceId)` → and use tools such
as `tree`, `search`, `props`, `screenshot`, `input`, and `action`.

For Codex/Copilot and other MCP clients, see the
[XamlMcp README](https://github.com/trrahul/XamlMcp).

# User Guide

TBD