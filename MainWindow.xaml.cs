// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices; // For DllImport
using WinRT;
using System.Xml.Linq;
using Microsoft.UI;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Vitalpad.Utils;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Vitalpad
{
    public sealed partial class MainWindow
    {
        private WindowsSystemDispatcherQueueHelper _mWsdqHelper; // See below for implementation.
        private MicaController _mBackdropController;
        private SystemBackdropConfiguration _mConfigurationSource;

        public MainWindow()
        {
            this.InitializeComponent();
            TrySetSystemBackdrop();
            Title = "Vitalpad";
            InitializeDataBindingSampleData();
        }
        
        private void InitializeDataBindingSampleData()
        {
            Helper.Tabs = new ObservableCollection<TabViewItem>();
            Helper.ActiveFiles = new Dictionary<StorageFile, string>();
            Helper.Active = new Dictionary<TabViewItem, KeyValuePair<StorageFile, string>>();

            for (var index = 0; index < 3; index++)
            {
                Helper.Tabs.Add(CreateNewTab());
            }
        }
        

        // creating a backdrop for Win11
        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool TrySetSystemBackdrop()
        {
            if (!MicaController.IsSupported())
                return false; // Mica is not supported on this system
            _mWsdqHelper = new WindowsSystemDispatcherQueueHelper();
            _mWsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Create the policy object.
            _mConfigurationSource = new SystemBackdropConfiguration();
            this.Activated += Window_Activated;
            this.Closed += Window_Closed;
            ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

            // Initial configuration state.
            _mConfigurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            _mBackdropController = new MicaController();
    
            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            _mBackdropController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            _mBackdropController.SetSystemBackdropConfiguration(_mConfigurationSource);
            return true; // succeeded

        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            _mConfigurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed
            // so it doesn't try to use this closed window.
            if (_mBackdropController != null)
            {
                _mBackdropController.Dispose();
                _mBackdropController = null;
            }
            this.Activated -= Window_Activated;
            _mConfigurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (_mConfigurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            _mConfigurationSource.Theme = ((FrameworkElement)this.Content).ActualTheme switch
            {
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                ElementTheme.Light => SystemBackdropTheme.Light,
                ElementTheme.Default => SystemBackdropTheme.Default,
                _ => _mConfigurationSource.Theme
            };
        }

        private void TabView_AddButtonClick(TabView sender, object args)
        {
            Helper.Tabs.Add(CreateNewTab());
        }

        private async void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Style = Microsoft.UI.Xaml.Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "Save your work?",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Don't Save",
                CloseButtonText = "Cancel",
                Content = "Unsaved changes will disappear.",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Secondary) return;
            if (!Helper.Active.ContainsKey(args.Item as TabViewItem))
            {
                Helper.Tabs.Remove(args.Item as TabViewItem);
                return;
            }
            var key = Helper.Active[args.Item as TabViewItem].Key;
            Helper.ActiveFiles.Remove(key);
            Helper.Tabs.Remove(args.Item as TabViewItem);
        }

        public static TabViewItem CreateNewTab()
        {
            var newItem = new TabViewItem
            {
                Header = "New Document",
                IconSource = new SymbolIconSource() { Symbol = Symbol.Document }
            };

            // The content of the tab is often a frame that contains a page, though it could be any UIElement.
            var frameTab = new Frame();
            frameTab.Navigate(typeof(EditorPage));
            newItem.Content = frameTab;
            return newItem;
        }
    }

    internal class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        private object _mDispatcherQueueController;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (_mDispatcherQueueController != null) return;
            DispatcherQueueOptions options;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
            options.threadType = 2;    // DQTYPE_THREAD_CURRENT
            options.apartmentType = 2; // DQTAT_COM_STA

            CreateDispatcherQueueController(options, ref _mDispatcherQueueController);
        }

    }

}
