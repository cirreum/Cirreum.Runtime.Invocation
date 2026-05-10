# Cirreum Runtime Invocation (umbrella)

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Runtime.Invocation.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.Invocation/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Runtime.Invocation.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.Invocation/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Runtime.Invocation?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Runtime.Invocation/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Runtime.Invocation?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Runtime.Invocation/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Umbrella Runtime Extensions package for the Cirreum Invocation source family.**

## Overview

`Cirreum.Runtime.Invocation` is the umbrella L5 Runtime Extensions package that composes every shipped Cirreum invocation source (SignalR, raw WebSocket, future gRPC) behind a single `AddInvocation()` / `MapInvocation()` pair. Install this one package when an application needs more than one invocation source; prefer the per-source sibling (`Cirreum.Runtime.Invocation.SignalR` or `Cirreum.Runtime.Invocation.WebSockets`) when only one is in use.

Mirrors the `Cirreum.Runtime.Identity` umbrella's shape exactly — same per-source composition pattern, same one-callback-invoked-once-against-a-shared-builder idempotency rule.

## When to install this vs. a per-source sibling

| Your app uses | Install |
|---|---|
| **Multiple invocation sources** (e.g. SignalR for browser dashboards + WebSocket for IVA voice) | This umbrella (`Cirreum.Runtime.Invocation`) |
| **SignalR only** | `Cirreum.Runtime.Invocation.SignalR` directly |
| **WebSocket only** | `Cirreum.Runtime.Invocation.WebSockets` directly |
| **HTTP only** (no long-lived sources) | Neither — HTTP is the framework default in `Cirreum.Services.Server` |

Don't install the umbrella *alongside* a per-source sibling — the umbrella already depends on both per-source packages, so doing so is redundant and may surface duplicate extension-method definitions at compile time.

## Architectural position

```
L2 Core
  Cirreum.InvocationProvider               ← abstractions: IInvocationContext, registrar base, IInvocationConnection.SendAsync, ...

L3 Infrastructure
  Cirreum.Invocation.SignalR               ← SignalR registrar, HubFilter, connection adapter
  Cirreum.Invocation.WebSockets            ← WebSocket registrar, orchestrator, connection adapter

L4 Runtime
  Cirreum.Runtime.InvocationProvider       ← IInvocationBuilder seam, RegisterInvocationProvider helper

L5 Runtime Extensions
  Cirreum.Runtime.Invocation.SignalR       ← AddSignalRInvocation, AddSignalR<THub>, MapSignalRInvocation
  Cirreum.Runtime.Invocation.WebSockets    ← AddWebSocketInvocation, AddWebSocket<THandler>, MapWebSocketInvocation
  Cirreum.Runtime.Invocation               ← THIS PACKAGE — AddInvocation, MapInvocation (composes both)
```

Same shape as the Identity track: `Cirreum.Runtime.Identity.Oidc` + `Cirreum.Runtime.Identity.EntraExternalId` per-protocol packages composed by the `Cirreum.Runtime.Identity` umbrella.

## What's in the box

| Extension | Lives on | Role |
|---|---|---|
| `AddInvocation(this IHostApplicationBuilder, Action<IInvocationBuilder>?)` (`Microsoft.Extensions.Hosting`) | `IHostApplicationBuilder` | Top-level entry point. Calls `AddSignalRInvocation()` and `AddWebSocketInvocation()` (no callback passed to either), then invokes the optional configure callback exactly once against a single `InvocationBuilder` so per-instance bindings (`AddSignalR<THub>(key)` / `AddWebSocket<THandler>(key, request: ...)`) run once per key — not once per source × key. |
| `MapInvocation(this IEndpointRouteBuilder)` (`Microsoft.AspNetCore.Builder`) | `IEndpointRouteBuilder` | Endpoints-phase entry point. Delegates to each per-source's `Map{Source}Invocation()` method so per-source side effects (e.g., `app.UseWebSockets()` for the WebSocket adapter) stay encapsulated where they belong. |

## How registration works

The `AddInvocation()` extension does three things:

1. **Calls `builder.AddSignalRInvocation()`** with no configure callback — registers the SignalR invocation source via the L4 `RegisterInvocationProvider<>` helper, binds `HubOptions` from `Cirreum:Invocation:Providers:SignalR:HubOptions`.
2. **Calls `builder.AddWebSocketInvocation()`** with no configure callback — registers the WebSocket invocation source, binds `WebSocketOptions` from `Cirreum:Invocation:Providers:WebSocket:WebSocketOptions`.
3. **Invokes the optional configure callback exactly once** against a single `InvocationBuilder(builder)` instance. Per-instance bindings like `b.AddSignalR<THub>("key")` / `b.AddWebSocket<THandler>("key", request: ...)` chain off this scope.

The configure callback is *not* forwarded to each per-source's `AddXxxInvocation()` — that would cause each `AddSignalR<THub>(k)` / `AddWebSocket<THandler>(k)` call to run twice (once inside each per-source AddXxx). Harmless (last-one-wins on duplicate-key dispatch) but wasteful. Same idempotency reasoning as the Identity umbrella's pattern.

## Quick start

```csharp
var builder = DomainApplication.CreateBuilder(args);

// Register every Cirreum invocation source the umbrella ships, then bind per-instance Hubs / handlers
builder.AddInvocation(b => b
    .AddSignalR<ChatHub>("chat")
    .AddSignalR<NotificationHub>("notifications")
    .AddWebSocket<TelemetryHandler>("telemetry")
    .AddWebSocket<TwilioMediaHandler>("media", request: r => r
        .Map(TwilioApi.HandleRequest, m => m
            .WithName("twilio-incoming-call")
            .Produces<string>(200, "text/xml"))));

var app = builder.Build();

// Map every invocation-source endpoint across all registered sources (SignalR Hubs + WebSocket endpoints + companion request endpoints)
app.MapInvocation();

await app.RunAsync();
```

Configuration drives which instances are enabled per source — see the per-source READMEs ([SignalR](https://github.com/cirreum/Cirreum.Runtime.Invocation.SignalR), [WebSocket](https://github.com/cirreum/Cirreum.Runtime.Invocation.WebSockets)) for the full per-source `Cirreum:Invocation:Providers:{Source}:Instances:*` shape.

## Dependencies

- **Cirreum.Runtime.Invocation.SignalR** `1.1.0+` — SignalR per-source L5 (brings L3 `Cirreum.Invocation.SignalR` + L4 `Cirreum.Runtime.InvocationProvider` transitively)
- **Cirreum.Runtime.Invocation.WebSockets** `1.1.0+` — WebSocket per-source L5 (brings L3 `Cirreum.Invocation.WebSockets` transitively)
- **Microsoft.AspNetCore.App** (framework reference) — for `IEndpointRouteBuilder` etc.

## Versioning

Follows [Semantic Versioning](https://semver.org/). Major bumps are coordinated with the per-source `Cirreum.Runtime.Invocation.*` packages.

## License

MIT — see [LICENSE](LICENSE).

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*
