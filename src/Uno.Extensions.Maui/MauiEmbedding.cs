//using Uno.Extensions.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Uno.Extensions.Maui.Internals;

namespace Uno.Extensions.Maui;

public static class MauiEmbedding
{
	private static MauiApp? _app;
#if ANDROID

	internal static IMauiContext MauiContext =>
		_app is not null ? new MauiContext(_app.Services, UI.ContextHelper.Current)
			: throw new MauiEmbeddingInitializationException();
#else
	internal static IMauiContext MauiContext =>
		_app is not null ? new MauiContext(_app.Services)
			: throw new MauiEmbeddingInitializationException();
#endif

	//public static IApplicationBuilder UseMauiEmbedding(this IApplicationBuilder builder, Action<MauiAppBuilder>? configure = null)
	//{
	//    builder.App.UseMauiEmbedding(configure);
	//    return builder;
	//}

#if NETSTANDARD
	public static void UseMauiEmbedding(this Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null)
	{
		throw new PlatformNotSupportedException();
	}
#else
	public static void UseMauiEmbedding(this Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null)
	{
		var mauiAppBuilder = MauiApp.CreateBuilder()
				.UseMauiApp<EmbeddingApp>();

		configure?.Invoke(mauiAppBuilder);

#if IOS || MACCATALYST
		mauiAppBuilder.Services.AddTransient<UIKit.UIWindow>(_ =>
			app.Window!);
#endif

		mauiAppBuilder.Services.AddSingleton(app)
			.AddSingleton<EmbeddingApp>();
		_app = mauiAppBuilder.Build();

		//// Hack: UseMauiEmbedding extension ignores the generic type for the application.
		//// https://github.com/dotnet/maui/blob/ae20455793142ae0034bcfdf8f810ca66aec782a/src/Compatibility/Core/src/AppHostBuilderExtensions.Embedding.cs#L12-L14
		var iApp = _app.Services.GetRequiredService<Microsoft.Maui.IApplication>();
		//if (iApp is Microsoft.Maui.Controls.Application mauiApp)
		//    mauiApp.Resources = app.Resources.ToMauiResources();
	}
#endif

	public static MauiAppBuilder MapControl<TWinUI, TMaui>(this MauiAppBuilder builder)
		where TWinUI : FrameworkElement
		where TMaui : Microsoft.Maui.Controls.View
	{
		Interop.MauiInterop.MapControl<TWinUI, TMaui>();
		return builder;
	}

	public static MauiAppBuilder MapStyleHandler<THandler>(this MauiAppBuilder builder)
		where THandler : Interop.IWinUIToMauiStyleHandler, new()
	{
		Interop.MauiInterop.MapStyleHandler<THandler>();
		return builder;
	}
}
