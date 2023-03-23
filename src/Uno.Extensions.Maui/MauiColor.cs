using Microsoft.Maui.Controls;

namespace Uno.Extensions.Maui;

[MarkupExtensionReturnType(ReturnType = typeof(NativeMauiColor))]
public class MauiColor : MauiExtensionBase
{
	public string Value { get; set; } = string.Empty;

	protected override void SetValue(View view, Type viewType, Type propertyType, BindableProperty property, string propertyName)
	{
		if (!string.IsNullOrEmpty(Value) || !NativeMauiColor.TryParse(Value, out var color))
		{
			return;
		}

		view.SetValue(property, color);
	}
}


