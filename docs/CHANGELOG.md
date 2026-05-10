# Cirreum.Runtime.Invocation Changelog

All notable changes to **Cirreum.Runtime.Invocation** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed migration steps on major version bumps, see the per-version migration
guides linked at the bottom of each entry.

---

## [Unreleased]

## [1.0.0] - 2026-05-10

### Added

Initial release of the umbrella Runtime Extensions package for the Cirreum Invocation source family. Composes `Cirreum.Runtime.Invocation.SignalR` and `Cirreum.Runtime.Invocation.WebSockets` behind a single `AddInvocation()` / `MapInvocation()` pair. Mirrors the `Cirreum.Runtime.Identity` umbrella's shape.

**Host application extensions (`Microsoft.Extensions.Hosting` namespace):**

- `AddInvocation(this IHostApplicationBuilder, Action<IInvocationBuilder>?)` — top-level entry point. Calls `builder.AddSignalRInvocation()` and `builder.AddWebSocketInvocation()` (no callback passed to either), then invokes the optional configure callback exactly once against a single `InvocationBuilder(builder)` so per-instance bindings (`AddSignalR<THub>(key)` / `AddWebSocket<THandler>(key, request: ...)`) chain through one shared scope.

**Endpoint extensions (`Microsoft.AspNetCore.Builder` namespace):**

- `MapInvocation(this IEndpointRouteBuilder)` — endpoints-phase entry point. Delegates to each per-source's `Map{Source}Invocation()` method so per-source side effects (e.g., `app.UseWebSockets()` for the WebSocket adapter) stay encapsulated where they belong. Does NOT hand-roll an unfiltered iteration of `InvocationProviderMapping` records — that would skip per-source pre-mapping side effects.

### Architecture position

This package is the **L5 Runtime Extensions umbrella** that composes the per-source L5 siblings (`Cirreum.Runtime.Invocation.SignalR` and `Cirreum.Runtime.Invocation.WebSockets`). Apps with a single invocation source (only SignalR, only WebSocket) install the per-source sibling directly to keep the binary lean; apps with multiple sources install this umbrella.

Same per-source composition pattern as the Identity track umbrella (`Cirreum.Runtime.Identity` composing `Cirreum.Runtime.Identity.Oidc` + `Cirreum.Runtime.Identity.EntraExternalId`). The umbrella deliberately violates the "L5 packages don't reference each other" rule that holds for non-umbrella packages — that's exactly its purpose.
