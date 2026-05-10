# Cirreum.Runtime.Invocation 1.0.0 — umbrella for the Invocation source family

Initial release. Composes the per-source L5 Runtime Extensions packages (`Cirreum.Runtime.Invocation.SignalR` and `Cirreum.Runtime.Invocation.WebSockets`) behind a single `AddInvocation()` / `MapInvocation()` pair. Mirrors the `Cirreum.Runtime.Identity` umbrella shape exactly.

---

## Why this release exists

Apps that use **more than one Cirreum invocation source** (e.g., SignalR for browser dashboards alongside WebSocket for Twilio IVA / voice) previously had to call both per-source extensions explicitly:

```csharp
// Before — per-source, explicit
builder.AddSignalRInvocation(b => b.AddSignalR<ChatHub>("chat"));
builder.AddWebSocketInvocation(b => b.AddWebSocket<TwilioMediaHandler>("media", request: r => r.Map(...)));

app.MapSignalRInvocation();
app.MapWebSocketInvocation();
```

That works, but it scales linearly with each new source the framework adds (gRPC streaming, future…), and it splits per-instance bindings across multiple `IInvocationBuilder` scopes. The umbrella collapses both:

```csharp
// After — umbrella, single scope
builder.AddInvocation(b => b
    .AddSignalR<ChatHub>("chat")
    .AddWebSocket<TwilioMediaHandler>("media", request: r => r.Map(...)));

app.MapInvocation();
```

When a future source ships (e.g., gRPC streaming via `Cirreum.Runtime.Invocation.Grpc`), the umbrella picks it up automatically — apps using `AddInvocation()` / `MapInvocation()` get the new source for free without code changes (just a config-side instance entry).

---

## What's new

### `AddInvocation(builder, configure?)` (`Microsoft.Extensions.Hosting` namespace)

Composes per-source registration:

1. Calls `builder.AddSignalRInvocation()` with no configure callback.
2. Calls `builder.AddWebSocketInvocation()` with no configure callback.
3. Invokes the optional `Action<IInvocationBuilder>` callback exactly once against a single `InvocationBuilder(builder)` instance.

The not-forwarding-the-callback rationale is the same as the Identity umbrella's: the per-instance binding methods (`AddSignalR<THub>(key)` / `AddWebSocket<THandler>(key)`) are idempotent per key but wasteful if invoked multiple times. Invoking the configure callback once against a shared scope keeps each binding from running once per source × key.

### `MapInvocation(endpoints)` (`Microsoft.AspNetCore.Builder` namespace)

Delegates to each per-source's `Map{Source}Invocation()` method:

```csharp
public static IEndpointRouteBuilder MapInvocation(this IEndpointRouteBuilder endpoints) {
    endpoints.MapSignalRInvocation();
    endpoints.MapWebSocketInvocation();
    return endpoints;
}
```

**Why delegation instead of unfiltered iteration of `InvocationProviderMapping`** (the way the Identity umbrella iterates `IdentityProviderMapping`): per-source `Map{Source}Invocation()` methods own pre-mapping side effects that the umbrella shouldn't replicate. The WebSocket adapter's `MapWebSocketInvocation()` calls `app.UseWebSockets()` internally before iterating its mappings; iterating the mappings unfiltered from the umbrella would skip that side effect, breaking WebSocket transport. Delegation preserves per-source encapsulation. Future sources can add their own setup (`UseRequestTimeouts()`, `UseGrpcWeb()`, whatever) inside their `Map*Invocation()` without the umbrella having to know.

---

## Quick start

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddInvocation(b => b
    .AddSignalR<ChatHub>("chat")
    .AddSignalR<NotificationHub>("notifications")
    .AddWebSocket<TelemetryHandler>("telemetry")
    .AddWebSocket<TwilioMediaHandler>("media", request: r => r
        .Map(TwilioApi.HandleRequest, m => m
            .WithName("twilio-incoming-call")
            .Produces<string>(200, "text/xml"))));

var app = builder.Build();
app.MapInvocation();
await app.RunAsync();
```

Configuration drives which instances are enabled per source. See the per-source READMEs for the full `Cirreum:Invocation:Providers:{Source}:Instances:*` shape.

---

## When to use this vs. a per-source sibling

| Your app uses | Install |
|---|---|
| **Multiple invocation sources** | This umbrella (`Cirreum.Runtime.Invocation`) |
| **SignalR only** | `Cirreum.Runtime.Invocation.SignalR` directly |
| **WebSocket only** | `Cirreum.Runtime.Invocation.WebSockets` directly |
| **HTTP only** | Neither — HTTP is the framework default in `Cirreum.Services.Server` |

Don't install the umbrella alongside a per-source sibling — the umbrella already depends on both per-source packages, so doing so is redundant and may surface duplicate extension-method definitions.

---

## Architectural position

```
L5 Runtime Extensions (composition layer)
  Cirreum.Runtime.Invocation.SignalR       per-source: AddSignalRInvocation, AddSignalR<THub>, MapSignalRInvocation
  Cirreum.Runtime.Invocation.WebSockets    per-source: AddWebSocketInvocation, AddWebSocket<THandler>, MapWebSocketInvocation
  Cirreum.Runtime.Invocation               THIS PACKAGE — umbrella over both
```

Mirrors the Identity track exactly: `Cirreum.Runtime.Identity` umbrella over `Cirreum.Runtime.Identity.Oidc` + `Cirreum.Runtime.Identity.EntraExternalId`.

The umbrella deliberately references other L5 packages (its per-source siblings) — that's its whole purpose. Cirreum's general "packages at the same layer don't reference each other" rule has an explicit exception for umbrellas, mirrored across both Identity and Invocation tracks.

---

## Coordinated work

Ships against the as-shipped state of the Invocation family (2026-05-10):

- **Cirreum.InvocationProvider 1.3.0** (L2) — `IConnectionSender` consolidated into `IInvocationConnection.SendAsync<T>`
- **Cirreum.Invocation.SignalR 1.2.1** (L3) — concrete SignalR registrar + auth-slot flow-through fix
- **Cirreum.Invocation.WebSockets 1.2.1** (L3) — concrete WebSocket registrar + auth-slot flow-through fix + `IWebSocketConnection`
- **Cirreum.Runtime.InvocationProvider 1.2.0** (L4) — `IInvocationBuilder`, `RegisterInvocationProvider<>` helper
- **Cirreum.Runtime.Invocation.SignalR 1.1.0** (L5 per-source)
- **Cirreum.Runtime.Invocation.WebSockets 1.1.0** (L5 per-source) — provider-level `WebSocketOptions` config

All flow in transitively.

---

## Compatibility

- **Source- and binary-compatible** for any future per-source addition (gRPC streaming, etc.) — apps using `AddInvocation()` / `MapInvocation()` pick up new sources automatically when this umbrella ships a minor that adds the new per-source dep.
- **No public API surface beyond the two extension methods** — the umbrella is composition only.

---

## See also

- [`Cirreum.Runtime.Invocation.SignalR`](https://www.nuget.org/packages/Cirreum.Runtime.Invocation.SignalR) — per-source L5
- [`Cirreum.Runtime.Invocation.WebSockets`](https://www.nuget.org/packages/Cirreum.Runtime.Invocation.WebSockets) — per-source L5
- [`Cirreum.Runtime.Identity`](https://www.nuget.org/packages/Cirreum.Runtime.Identity) — sibling umbrella that this package's shape mirrors
- [ADR-0002](https://github.com/cirreum/Cirreum.DevOps/blob/main/docs/adr/0002-unified-invocation-context.md) — the foundational unified `IInvocationContext` seam decision that organizes the whole Invocation family
