﻿using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;

namespace Uno.Extensions.Navigation.UI;

public class NavigationViewRequestHandler : ControlRequestHandlerBase<NavigationView>
{
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewToBind = view;
		var viewList = view as NavigationView;
		if (viewList is null)
		{
			return default;
		}

		Func<FrameworkElement, object, Task> action = async (sender, data) =>
		{
			var navdata = sender.GetData() ?? data;
			var path = sender.GetRequest();
			var nav = sender.Navigator();
			if (nav is null)
			{
				return;
			}

			await nav.NavigateRouteAsync(sender, path, Qualifiers.None, navdata);
		};

		TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs> selectionAction = async (actionSender, actionArgs) =>
		{
			var sender = actionSender;
			if (sender is null)
			{
				return;
			}

			if (actionArgs.SelectedItemContainer is NavigationViewItem navItem && !navItem.SelectsOnInvoked)
			{
				return;
			}

			var data = sender.GetData() ?? sender.SelectedItem;
			if (data is null)
			{
				return;
			}

			await action(sender, data);
		};

		TypedEventHandler<NavigationView, NavigationViewItemInvokedEventArgs> clickAction = async (actionSender, actionArgs) =>
		{
			var sender = actionSender;
			if (sender is null)
			{
				return;
			}

			if(actionArgs.InvokedItemContainer is NavigationViewItem navItem && navItem.SelectsOnInvoked)
			{
				return;
			}

			var data = sender.GetData() ?? actionArgs.InvokedItem;
			if (data is null)
			{
				return;
			}

			await action(sender, data);
		};

		Action? connect = null;
		Action? disconnect = null;

		connect = () =>
		{
			viewList.ItemInvoked += clickAction;
			viewList.SelectionChanged += selectionAction;

			if (viewList.SelectedItem is not null)
			{
				action(viewList, viewList.SelectedItem);
			}
		};

		disconnect = () =>
		{
			viewList.ItemInvoked -= clickAction;
			viewList.SelectionChanged -= selectionAction;
		};


		if (viewList.IsLoaded)
		{
			connect();
		}

		RoutedEventHandler loadedHandler = (s, e) =>
		{
			connect();
		};
		viewList.Loaded += loadedHandler;
		RoutedEventHandler unloadedHandler = (s, e) =>
		{
			disconnect();
		};
		viewList.Unloaded += unloadedHandler;
		return new RequestBinding(viewToBind, loadedHandler, unloadedHandler);
	}
}
