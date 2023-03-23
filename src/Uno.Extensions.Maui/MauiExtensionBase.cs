using System.Diagnostics;
using System.Reflection;
using Microsoft.Maui.Controls;

namespace Uno.Extensions.Maui;

public abstract class MauiExtensionBase : MarkupExtension
{
	protected sealed override object? ProvideValue(IXamlServiceProvider serviceProvider)
	{
		var provideValueTarget = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

		if (provideValueTarget?.TargetObject is View view && provideValueTarget.TargetProperty is ProvideValueTargetProperty targetProperty)
		{
			var declaringType = targetProperty.DeclaringType;
			var targetType = declaringType.GetRuntimeProperty(targetProperty.Name)?.PropertyType;

			void OnParented(object? sender, EventArgs args)
			{
				view.ParentChanged -= OnParented;
				var bindablePropertyInfo = targetProperty.DeclaringType.GetRuntimeField($"{targetProperty.Name}Property");
				var bindableProperty = bindablePropertyInfo?.GetValue(null) as Microsoft.Maui.Controls.BindableProperty;

				var name = targetProperty.Name;

				if (targetType is null || bindableProperty is null)
				{
					// TODO: Add Logging
					Debug.Assert(targetType is null, "The Target Type is null");
					Debug.Assert(bindableProperty is null, "The BindableProperty is null");
#if DEBUG
					System.Diagnostics.Debugger.Break();
#endif
					return;
				}

				SetValue(view, declaringType, targetType, bindableProperty, name);
			}
			view.ParentChanged += OnParented;

			if (targetType is not null)
				return Default(targetType);
		}

		return base.ProvideValue(serviceProvider);
	}

	protected abstract void SetValue(View view, Type viewType, Type propertyType, BindableProperty property, string propertyName);

	protected object? Default(Type type) =>
		type.IsValueType ? Activator.CreateInstance(type) : null;
}
