namespace Uno.Extensions.Maui.Interop;

internal class ViewStyleHandler : WinUIToMauiStyleHandler
{
	public override Type TargetType => typeof(Microsoft.Maui.Controls.View);

	public override (Microsoft.Maui.Controls.BindableProperty Property, object? Value)? Process(DependencyProperty property, object value)
	{
		if (property == Control.MarginProperty)
		{
			return (Microsoft.Maui.Controls.View.MarginProperty, ConvertToMauiValue(value));
		}

		return null;
	}
}
