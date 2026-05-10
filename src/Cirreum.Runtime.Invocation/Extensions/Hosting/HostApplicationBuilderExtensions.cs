namespace Microsoft.Extensions.Hosting;

using Cirreum.Invocation;

/// <summary>
/// App-facing umbrella extensions for the Cirreum Invocation source family — composes
/// the per-source Runtime Extensions packages (SignalR, WebSockets, future gRPC) behind
/// a single <c>AddInvocation()</c> entry point.
/// </summary>
public static class HostApplicationBuilderExtensions {

	/// <summary>
	/// Registers every Cirreum invocation source shipped in the umbrella (SignalR and
	/// WebSocket). Enabled instances from configuration for each source are wired up;
	/// the optional <paramref name="configure"/> callback lets the app register
	/// per-instance Hub / handler bindings once, across all sources via the same
	/// <see cref="IInvocationBuilder"/> scope.
	/// </summary>
	/// <param name="builder">The host application builder.</param>
	/// <param name="configure">
	/// Optional callback to register per-instance source bindings using the fluent
	/// <c>AddSignalR&lt;THub&gt;(key)</c> / <c>AddWebSocket&lt;THandler&gt;(key, request: ...)</c>
	/// extension methods on <see cref="IInvocationBuilder"/>.
	/// </param>
	/// <returns>The host application builder for chaining.</returns>
	/// <example>
	/// <code>
	/// builder.AddInvocation(b => b
	///     .AddSignalR&lt;ChatHub&gt;("chat")
	///     .AddSignalR&lt;NotificationHub&gt;("notifications")
	///     .AddWebSocket&lt;TwilioMediaHandler&gt;("media", request: r => r
	///         .Map(TwilioApi.HandleRequest)));
	/// </code>
	/// </example>
	public static IHostApplicationBuilder AddInvocation(
		this IHostApplicationBuilder builder,
		Action<IInvocationBuilder>? configure = null) {

		// Register both sources without passing the configure callback — we invoke it
		// exactly once below against a single InvocationBuilder so per-instance
		// bindings (AddSignalR<THub>, AddWebSocket<THandler>) run once per key (not
		// once per source × key).
		builder.AddSignalRInvocation();
		builder.AddWebSocketInvocation();

		configure?.Invoke(new InvocationBuilder(builder));
		return builder;
	}

}
