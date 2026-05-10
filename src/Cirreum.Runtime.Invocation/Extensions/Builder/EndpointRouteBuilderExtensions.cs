namespace Microsoft.AspNetCore.Builder;

using Microsoft.AspNetCore.Routing;

/// <summary>
/// App-facing umbrella extensions for mapping every Cirreum invocation-source endpoint
/// across all registered sources.
/// </summary>
public static class EndpointRouteBuilderExtensions {

	/// <summary>
	/// Maps every enabled invocation-source endpoint across every registered Cirreum
	/// invocation source (SignalR and WebSocket). Delegates to each per-source's
	/// <c>Map{Source}Invocation()</c> method so per-source side effects (e.g.,
	/// <c>UseWebSockets()</c> for the WebSocket adapter) stay encapsulated where they
	/// belong rather than being duplicated here.
	/// </summary>
	/// <param name="endpoints">The endpoint route builder.</param>
	/// <returns>The endpoint route builder for chaining.</returns>
	/// <remarks>
	/// Apps installing this umbrella package call <c>app.MapInvocation()</c> instead of
	/// the per-source <c>app.MapSignalRInvocation()</c> / <c>app.MapWebSocketInvocation()</c>.
	/// The umbrella does not hand-roll an unfiltered iteration of
	/// <c>InvocationProviderMapping</c> records because each per-source's Map method
	/// owns its own pre-mapping side effects (the WebSocket adapter calls
	/// <c>app.UseWebSockets()</c> internally before iterating its mappings).
	/// </remarks>
	public static IEndpointRouteBuilder MapInvocation(this IEndpointRouteBuilder endpoints) {

		endpoints.MapSignalRInvocation();
		endpoints.MapWebSocketInvocation();

		return endpoints;
	}

}
