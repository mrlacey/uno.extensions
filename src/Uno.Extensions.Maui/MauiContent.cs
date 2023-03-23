using Microsoft.UI.Xaml.Media;
using Uno.Extensions.Maui.Internals;

namespace Uno.Extensions.Maui;

[ContentProperty(Name = nameof(View))]
public partial class MauiContent : ContentControl
{
	public static readonly DependencyProperty ViewProperty =
		DependencyProperty.Register(nameof(View), typeof(View), typeof(MauiContent), new PropertyMetadata(OnViewChanged));

	private static void OnViewChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if (args.NewValue is null || args.NewValue is not View view || dependencyObject is not MauiContent embeddedView)
		{
			return;
		}

		if(embeddedView._host is not null)
		{
			view.Parent = embeddedView._host;
		}

		try
		{
			var native = view.ToPlatform(embeddedView.MauiContext);
			embeddedView.Content = native;
		}
		catch (Exception ex)
		{
#if DEBUG
			Console.WriteLine(ex);
			System.Diagnostics.Debugger.Break();
#endif
			throw new MauiEmbeddingException("An unexpected error occurred while converting the Maui View to a native View.", ex);
		}
	}

	private UnoHost? _host;

	private readonly IMauiContext MauiContext;

	public MauiContent()
	{
		MauiContext = MauiEmbedding.MauiContext;
		this.Loading += OnLoading;
	}

	public View View
	{
		get => (View)GetValue(ViewProperty);
		set => SetValue(ViewProperty, value);
	}

	private void OnLoading(FrameworkElement sender, object args)
	{
		Loading -= OnLoading;
		DependencyObject? treeElement = this;
		var resources = new ResourceDictionary();
		while(treeElement is not null)
		{
			if(treeElement is FrameworkElement element && element.Resources.Any())
			{
				foreach((var key, var value) in element.Resources)
				{
					if (resources.ContainsKey(key))
						continue;
					resources[key] = value;
				}
			}

			treeElement = VisualTreeHelper.GetParent(treeElement);
		}

		_host = new UnoHost(resources);
		var binding = new NativeMauiBinding(nameof(DataContext), BindingMode.OneWay, source: this);
		_host.SetBinding(UnoHost.BindingContextProperty, binding);

		if (View.Parent is null)
		{
			View.Parent = _host;
		}
	}
}
