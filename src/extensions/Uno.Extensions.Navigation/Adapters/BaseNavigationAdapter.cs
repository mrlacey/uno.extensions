﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Adapters
{
    public abstract class BaseNavigationAdapter : INavigationAdapter
    {
        protected IControlNavigation ControlWrapper { get; }

        public virtual NavigationContext CurrentContext { get; protected set; }

        public string Name { get; set; }

        protected INavigationMapping Mapping { get; }

        public IServiceProvider Services { get; }

        private IDialogProvider DialogProvider { get; }

        public INavigationService Navigation { get; set; }

        protected Stack<Dialog> OpenDialogs { get; } = new Stack<Dialog>();

        public virtual bool CanGoBack => false;

        public void Inject(object control)
        {
            ControlWrapper.Inject(control);
        }

        public virtual bool IsCurrentPath(string path)
        {
            return CurrentContext.Path == path;
        }

        public BaseNavigationAdapter(
            // INavigationService navigation, // Note: Don't pass in - implement INaviationAware instead
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IControlNavigation control)
        {
            Services = services.CreateScope().ServiceProvider;
            DialogProvider = Services.GetService<IDialogProvider>();
            Mapping = navigationMapping;
            ControlWrapper = control;
        }

        public NavigationResponse Navigate(NavigationContext context)
        {
            var request = context.Request;

            var navTask = InternalNavigate(context);

            return new NavigationResponse(request, navTask, context.CancellationSource, context.ResultCompletion.Task);
        }

        private async Task InternalNavigate(NavigationContext context)
        {
            var navigationHandled = await EndCurrentNavigationContext(context);

            if (context.CancellationToken.IsCancellationRequested)
            {
                await Task.FromCanceled(context.CancellationToken);
            }

            if (!navigationHandled)
            {
                PreNavigation(context);

                if (context.IsBackNavigation)
                {
                    await DoBackNavigation(context);
                }
                else
                {
                    await DoForwardNavigation(context);
                }

            }

            context = context with { CanCancel = this.CanGoBack || OpenDialogs.Any() };

            if (context.CanCancel)
            {
                context.CancellationToken.Register(() =>
                {
                    Navigation.NavigateToPreviousView(context.Request.Sender);
                });
            }
        }


        protected virtual async Task DoBackNavigation(NavigationContext context)
        {
            throw new NotSupportedException();
        }

        protected async Task DoForwardNavigation(NavigationContext context)
        {
            var mapping = Mapping.LookupByPath(context.Path);
            if (mapping is not null)
            {
                context = context with { Mapping = mapping };
            }

            var vm = await context.InitializeViewModel();

            var dialog = DialogProvider.CreateDialog(Navigation, context, vm);
            if (dialog is not null)
            {
                OpenDialogs.Push(dialog);
            }
            else
            {
                AdapterNavigation(context, vm);
            }

            await ((vm as INavigationStart)?.Start(context, true) ?? Task.CompletedTask);
        }

        protected virtual void PreNavigation(NavigationContext context)
        {
        }

        protected virtual void AdapterNavigation(NavigationContext context, object viewModel)
        {
            CurrentContext = context;
            ControlWrapper.Navigate(context, false, viewModel);
        }

        protected async Task<bool> EndCurrentNavigationContext(NavigationContext navigationContext)
        {
            // If this is back navigation, then make sure it's used to close
            // any of the open dialogs
            if (navigationContext.IsBackNavigation && OpenDialogs.Any())
            {
                await CloseDialog(navigationContext);
                return true;
            }

            // If there's a current nav context, make sure it's stopped before
            // we proceed - this could cancel the navigation, so need to know
            // before we remove anything from backstack
            if (CurrentContext is not null)
            {
                // Stop the currently active viewmodel
                await CurrentContext.StopVieModel(navigationContext);

                // Check if navigation was cancelled - if it is,
                // then indicate that navigation has been handled
                if (navigationContext.IsCancelled)
                {
                    return true;
                }

                if (navigationContext.IsBackNavigation)
                {
                    var responseData = navigationContext.Data.TryGetValue(string.Empty, out var response) ? response : default;

                    var context = CurrentContext;

                    if (context.Request.Result is not null)
                    {
                        var completion = context.ResultCompletion;
                        if (completion is not null)
                        {
                            completion.SetResult(responseData);
                        }
                    }
                }
            }

            return false;
        }

        protected async Task CloseDialog(NavigationContext navigationContext)
        {
            var dialog = OpenDialogs.Pop();

            var responseData = navigationContext.Data.TryGetValue(string.Empty, out var response) ? response : default;
            await dialog.Context.StopVieModel(navigationContext);


            responseData = dialog.Manager.CloseDialog(dialog, navigationContext, responseData);

            if (dialog.Context.Request.Result is not null)
            {
                var completion = dialog.Context.ResultCompletion;
                if (completion is not null)
                {
                    completion.SetResult(responseData);
                }
            }

            // Restart the view model for the current context
            var currentVM = await CurrentContext.InitializeViewModel();

            await ((currentVM as INavigationStart)?.Start(CurrentContext, false) ?? Task.CompletedTask);
        }
    }



    public static class NavigationContextHelpers
    {
        public static async Task<object> StopVieModel(this NavigationContext contextToStop, NavigationContext navigationContext)
        {
            object oldVm = default;
            if (contextToStop.Mapping?.ViewModel is not null)
            {
                var services = contextToStop.Services;
                oldVm = services.GetService(contextToStop.Mapping.ViewModel);
                await ((oldVm as INavigationStop)?.Stop(navigationContext, navigationContext.IsBackNavigation) ?? Task.CompletedTask);
            }
            return oldVm;
        }

        public static async Task<object> InitializeViewModel(this NavigationContext contextToInitialize)
        {
            var mapping = contextToInitialize.Mapping;
            object vm = default;
            if (mapping?.ViewModel is not null)
            {
                var services = contextToInitialize.Services;
                var dataFactor = services.GetService<ViewModelDataProvider>();
                dataFactor.Parameters = contextToInitialize.Data;

                vm = services.GetService(mapping.ViewModel);
                await ((vm as IInitialise)?.Initialize(contextToInitialize) ?? Task.CompletedTask);
            }
            return vm;
        }
    }
}
