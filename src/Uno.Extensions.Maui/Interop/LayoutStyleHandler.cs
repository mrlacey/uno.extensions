namespace Uno.Extensions.Maui.Interop;

internal class LayoutStyleHandler : WinUIToMauiStyleHandler
{
	public override Type TargetType => typeof(Microsoft.Maui.Controls.Layout);

	public override (Microsoft.Maui.Controls.BindableProperty Property, object? Value)? Process(DependencyProperty property, object value)
	{
		if (property == Control.PaddingProperty)
		{
			return (Microsoft.Maui.Controls.Layout.PaddingProperty, ConvertToMauiValue(value));
		}

		return null;
	}
}
