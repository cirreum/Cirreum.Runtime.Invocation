# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **Cirreum.Runtime.Invocation** — the umbrella Runtime Extensions package for the Cirreum Invocation source family. It composes the per-source packages (`Cirreum.Runtime.Invocation.SignalR` and `Cirreum.Runtime.Invocation.WebSockets`) behind a single `AddInvocation()` / `MapInvocation()` pair. Install this one package when the application needs multiple invocation sources; prefer a per-source package when a single source is used.

## Build Commands

```bash
dotnet build Cirreum.Runtime.Invocation.slnx
dotnet pack --configuration Release
```

## Architecture

### What this package does

1. **`AddInvocation(builder, configure?)`** (`Extensions/Hosting/HostApplicationBuilderExtensions.cs`)
   - Calls `builder.AddSignalRInvocation()` (no callback passed).
   - Calls `builder.AddWebSocketInvocation()` (no callback passed).
   - Invokes the optional `Action<IInvocationBuilder>` callback exactly once against a single `InvocationBuilder(builder)` instance.
   - Rationale for not forwarding the callback to each per-source method: the callback typically registers per-instance source bindings via `AddSignalR<THub>(key)` / `AddWebSocket<THandler>(key, request: ...)`. Forwarding would cause each binding to run twice (once inside each per-source `AddXxxInvocation`) — harmless (last-one-wins on dispatch) but wasteful.

2. **`MapInvocation(endpoints)`** (`Extensions/Builder/EndpointRouteBuilderExtensions.cs`)
   - Delegates to each per-source's `Map{Source}Invocation()` method:
     - `endpoints.MapSignalRInvocation()` — maps SignalR Hub endpoints
     - `endpoints.MapWebSocketInvocation()` — calls `app.UseWebSockets()` then maps WebSocket endpoints
   - **Does NOT iterate `IEnumerable<InvocationProviderMapping>` unfiltered** (the way the Identity umbrella iterates `IdentityProviderMapping`). The per-source `Map{Source}Invocation()` methods own pre-mapping side effects — most notably `app.UseWebSockets()` — and delegating preserves that encapsulation. Iterating unfiltered would skip the side effects (the umbrella would have to duplicate `UseWebSockets()` itself), violating per-source encapsulation.

### What this package does NOT do

- **Does not re-implement any Add/Map logic** — all behavior comes transitively from `Cirreum.Runtime.Invocation.SignalR` and `Cirreum.Runtime.Invocation.WebSockets`.
- **Does not register any source directly** — it only composes the per-source packages that do.

## Project Structure

```
src/Cirreum.Runtime.Invocation/
├── Extensions/
│   ├── Hosting/
│   │   └── HostApplicationBuilderExtensions.cs   # AddInvocation (umbrella)
│   └── Builder/
│       └── EndpointRouteBuilderExtensions.cs     # MapInvocation (umbrella)
└── Cirreum.Runtime.Invocation.csproj
```

`RootNamespace` = `Cirreum.Runtime`, with extension classes in `Microsoft.Extensions.Hosting` / `Microsoft.AspNetCore.Builder` for discoverability.

## Dependencies

- **Cirreum.Runtime.Invocation.SignalR** (brings `Cirreum.Invocation.SignalR` + `Cirreum.Runtime.InvocationProvider` transitively)
- **Cirreum.Runtime.Invocation.WebSockets** (brings `Cirreum.Invocation.WebSockets` transitively)
- **Microsoft.AspNetCore.App**

## When to use this package vs. a per-source sibling

- **Single source:** install `Cirreum.Runtime.Invocation.SignalR` OR `Cirreum.Runtime.Invocation.WebSockets` directly. The binary only carries that source's infra code.
- **Multiple sources:** install this umbrella. Both sources' infra flows in transitively.
- **Never install the umbrella alongside a per-source package** — the umbrella already depends on both, so doing so is redundant and may surface duplicate extension-method definitions at compile time.
- **HTTP-only apps:** install neither — HTTP is the framework default in `Cirreum.Services.Server` and doesn't need a per-source package.

## Layer rule exception

Cirreum's layer model says "Foundational L2 packages do NOT reference each other" and L3 packages can reference L2 only. The umbrella L5 package **deliberately references other L5 packages** (its per-source siblings) — that's its whole purpose. Same exception holds for the Identity umbrella (`Cirreum.Runtime.Identity` references `Cirreum.Runtime.Identity.Oidc` + `Cirreum.Runtime.Identity.EntraExternalId`).

## Development Notes

- Uses .NET 10.0 with latest C# language version
- Nullable reference types enabled
- Extremely thin — two methods, both composition-only
- File-scoped namespaces
- K&R braces, tabs for indentation (matches repo `.editorconfig`)
