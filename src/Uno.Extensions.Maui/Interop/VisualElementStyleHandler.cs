namespace Uno.Extensions.Maui.Interop;

internal class VisualElementStyleHandler : WinUIToMauiStyleHandler
{
	public override Type TargetType => typeof(Microsoft.Maui.Controls.VisualElement);

	public override (Microsoft.Maui.Controls.BindableProperty Property, object? Value)? Process(DependencyProperty property, object value)
	{
		if (property == Control.BackgroundProperty)
		{
			return (Microsoft.Maui.Controls.VisualElement.BackgroundProperty, ConvertToMauiValue(value));
		}

		return null;
	}
}
