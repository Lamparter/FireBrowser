using CommunityToolkit.Mvvm.ComponentModel;
using FireBrowserBusiness.Controls;
using FireBrowserBusiness.Pages;
using FireBrowserDatabase;
using FireBrowserFavorites;
using FireBrowserMultiCore;
using FireBrowserQr;
using FireBrowserWinUi3.Controls;
using FireBrowserWinUi3.Pages;
using FireBrowserWinUi3.Pages.TimeLinePages;
using Microsoft.Data.Sqlite;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UrlHelperWinUi3;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using WinRT.Interop;
using Windowing = FireBrowserBusinessCore.Helpers.Windowing;

namespace FireBrowserBusiness;
public sealed partial class MainWindow : Window
{
    public class StringOrIntTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate IntTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is string)
            {
                return StringTemplate;
            }
            else if (item is int)
            {
                return IntTemplate;
            }
            else
            {
                return DefaultTemplate;
            }
        }
    }

    private AppWindow appWindow;
    private AppWindowTitleBar titleBar;

    public DownloadFlyout DownloadFlyout { get; set; } = new DownloadFlyout();

    public MainWindow()
    {
        InitializeComponent();

        if (App.Args == string.Empty | App.Args == null) // Main view
        {
            Tabs.TabItems.Add(CreateNewTab(typeof(NewTab)));
        }
        else if (App.Args.Contains("firebrowserwinui://")) // PWA/Pinned websites view
        {
            Tabs.TabItems.Add(CreateNewTab(typeof(InPrivate)));
        }
        else if (App.Args.Contains("firebrowserwinui://private"))
        {
            Tabs.TabItems.Add(CreateNewIncog(typeof(InPrivate)));
        }


        TitleTop();
        LoadUserDataAndSettings();
        LoadUsernames();
    }

    private void LoadUsernames()
    {
        List<string> usernames = AuthService.GetAllUsernames();
        string currentUsername = AuthService.CurrentUser?.Username;

        foreach (string username in usernames)
        {
            // Exclude the current user's username
            if (username != currentUsername)
            {
                UserListView.Items.Add(username);
            }
        }
    }
    public void SmallUpdates()
    {
        UrlBox.Text = TabWebView.CoreWebView2.Source.ToString();
        ViewModel.Securitytype = TabWebView.CoreWebView2.Source.ToString();

        if (TabWebView.CoreWebView2.Source.Contains("https"))
        {
            ViewModel.SecurityIcon = "\uE72E";
            ViewModel.SecurityIcontext = "Https Secured Website";
            ViewModel.Securitytext = "This Page Is Secured By A Valid SSL Certificate, Trusted By Root Authorities";
        }
        else if (TabWebView.CoreWebView2.Source.Contains("http"))
        {
            ViewModel.SecurityIcon = "\uE785";
            ViewModel.SecurityIcontext = "Http UnSecured Website";
            ViewModel.Securitytext = "This Page Is Unsecured By A Un-Valid SSL Certificate, Please Be Careful";
        }
    }

    public void TitleTop()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.SetIcon("Logo.ico");


        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            throw new Exception("Unsupported OS version.");
        }

        titleBar = appWindow.TitleBar;
        titleBar.ExtendsContentIntoTitleBar = true;
        var btnColor = Colors.Transparent;
        titleBar.BackgroundColor = btnColor;
        titleBar.ButtonBackgroundColor = btnColor;
        titleBar.InactiveBackgroundColor = btnColor;
        titleBar.ButtonInactiveBackgroundColor = btnColor;
        titleBar.ButtonHoverBackgroundColor = btnColor;

        ViewModel = new ToolbarViewModel
        {
            currentAddress = "",
            SecurityIcon = "\uE946",
            SecurityIcontext = "FireBrowser Home Page",
            Securitytext = "This The Default Home Page Of Firebrowser Internal Pages Secure",
            Securitytype = "Link - FireBrowser://NewTab"
        };


        buttons();
    }


    public static string launchurl { get; set; }
    public static string SearchUrl { get; set; }

    public bool isFull = false;

    public void GoFullScreenWeb(bool fullscreen)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var view = AppWindow.GetFromWindowId(wndId);
        var margin = fullscreen ? new Thickness(0, -40, 0, 0) : new Thickness(0, 35, 0, 0);

        if (fullscreen)
        {
            view.SetPresenter(AppWindowPresenterKind.FullScreen);

            ClassicToolbar.Height = 0;

            TabContent.Margin = margin;
        }
        else
        {
            view.SetPresenter(AppWindowPresenterKind.Default);

            ClassicToolbar.Height = 40;

            TabContent.Margin = margin;
        }
    }

    public void GoFullScreen(bool fullscreen)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var view = AppWindow.GetFromWindowId(wndId);

        if (isFull == false)
        {
            view.SetPresenter(AppWindowPresenterKind.FullScreen);
            isFull = true;
            TextFull.Text = "Exit FullScreen";
        }
        else
        {
            view.SetPresenter(AppWindowPresenterKind.Default);
            isFull = false;
            TextFull.Text = "Full Screen";
        }
    }


    Settings userSettings = UserFolderManager.LoadUserSettings(AuthService.CurrentUser);
    private void LoadUserDataAndSettings()
    {
        if (GetUser() is not { } currentUser)
        {
            UserName.Text = Prof.Text = "DefaultUser";
            return;
        }

        if (!AuthService.IsUserAuthenticated && !AuthService.Authenticate(currentUser.Username))
        {
            return;
        }

        UserName.Text = Prof.Text = AuthService.CurrentUser?.Username ?? "DefaultUser";
    }




    public void buttons()
    {
        SetVisibility(AdBlock, userSettings.AdblockBtn != "0");
        SetVisibility(ReadBtn, userSettings.ReadButton != "0");
        SetVisibility(BtnTrans, userSettings.Translate != "0");
        SetVisibility(BtnDark, userSettings.DarkIcon != "0");
        SetVisibility(ToolBoxMore, userSettings.ToolIcon != "0");
        SetVisibility(AddFav, userSettings.FavoritesL != "0");
        SetVisibility(FavoritesButton, userSettings.Favorites != "0");
        SetVisibility(DownBtn, userSettings.Downloads != "0");
        SetVisibility(History, userSettings.Historybtn != "0");
        SetVisibility(QrBtn, userSettings.QrCode != "0");
    }

    private void SetVisibility(UIElement element, bool isVisible)
    {
        element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }


    private FireBrowserMultiCore.User GetUser()
    {
        // Check if the user is authenticated.
        if (AuthService.IsUserAuthenticated)
        {
            // Return the authenticated user.
            return AuthService.CurrentUser;
        }

        // If no user is authenticated, return null or handle as needed.
        return null;
    }


    private void TabView_AddTabButtonClick(TabView sender, object args)
    {
        sender.TabItems.Add(CreateNewTab(typeof(NewTab)));
    }

    #region toolbar
    public class Passer
    {
        public FireBrowserTabViewItem Tab { get; set; }
        public FireBrowserTabViewContainer TabView { get; set; }
        public object Param { get; set; }

        public ToolbarViewModel ViewModel { get; set; }
        public string UserName { get; set; }
    }

    public ToolbarViewModel ViewModel { get; set; }

    public partial class ToolbarViewModel : ObservableObject
    {
        [ObservableProperty]
        public bool canRefresh;
        [ObservableProperty]
        public bool canGoBack;
        [ObservableProperty]
        public bool canGoForward;
        [ObservableProperty]
        public string currentAddress;
        [ObservableProperty]
        public string securityIcon;
        [ObservableProperty]
        public string securityIcontext;
        [ObservableProperty]
        public string securitytext;
        [ObservableProperty]
        public string securitytype;
        [ObservableProperty]
        public Visibility homeButtonVisibility;

        private string _userName;

        public string UserName
        {
            get
            {
                if (_userName == "DefaultFireBrowserUser") return "DefaultFireBrowserUserName";
                else return _userName;
            }
            set { SetProperty(ref _userName, value); }
        }
    }

    #endregion

    public FireBrowserTabViewItem CreateNewTab(Type page = null, object param = null, int index = -1)
    {
        if (index == -1) index = Tabs.TabItems.Count;

        var newItem = new FireBrowserTabViewItem
        {
            Header = "FireBrowser HomePage",
            IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource { Symbol = Symbol.Home },
            Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["FloatingTabViewItemStyle"]
        };


        Passer passer = new()
        {
            Tab = newItem,
            TabView = Tabs,
            ViewModel = new ToolbarViewModel(),
            Param = param,
        };

        passer.ViewModel.CurrentAddress = null;

        double margin = ClassicToolbar.Height;
        var frame = new Frame
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, margin, 0, 0)
        };

        if (page != null)
        {
            frame.Navigate(page, passer);
        }

        var toolTip = new ToolTip();
        toolTip.Content = new Grid
        {
            Children =
        {
            new Microsoft.UI.Xaml.Controls.Image(),
            new TextBlock()
        }
        };
        ToolTipService.SetToolTip(newItem, toolTip);

        newItem.Content = frame;
        return newItem;
    }

    public Frame TabContent
    {
        get
        {
            FireBrowserTabViewItem selectedItem = (FireBrowserTabViewItem)Tabs.SelectedItem;
            if (selectedItem != null)
            {
                return (Frame)selectedItem.Content;
            }
            return null;
        }
    }

    public WebView2 TabWebView
    {
        get
        {
            if (TabContent.Content is WebContent)
            {
                return (TabContent.Content as WebContent).WebViewElement;
            }
            return null;
        }
    }



    private double GetScaleAdjustment()
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
        IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

        int result = Windowing.GetDpiForMonitor(hMonitor, Windowing.Monitor_DPI_Type.MDT_Default_DPI, out uint dpiX, out _);

        if (result != 0)
        {
            throw new Exception("Could not get DPI");
        }

        return dpiX / 96.0; // Simplified calculation
    }

    private void Tabs_Loaded(object sender, RoutedEventArgs e)
    {
        Apptitlebar.SizeChanged += Apptitlebar_SizeChanged;
    }

    private void Apptitlebar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        double scaleAdjustment = GetScaleAdjustment();
        Apptitlebar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var customDragRegionPosition = Apptitlebar.TransformToVisual(null).TransformPoint(new Point(0, 0));

        var dragRects = new Windows.Graphics.RectInt32[2];

        for (int i = 0; i < 2; i++)
        {
            dragRects[i] = new Windows.Graphics.RectInt32
            {
                X = (int)((customDragRegionPosition.X + (i * Apptitlebar.ActualWidth / 2)) * scaleAdjustment),
                Y = (int)(customDragRegionPosition.Y * scaleAdjustment),
                Height = (int)((Apptitlebar.ActualHeight - customDragRegionPosition.Y) * scaleAdjustment),
                Width = (int)((Apptitlebar.ActualWidth / 2) * scaleAdjustment)
            };
        }

        appWindow.TitleBar?.SetDragRectangles(dragRects);
    }

    private void Apptitlebar_LayoutUpdated(object sender, object e)
    {
        double scaleAdjustment = GetScaleAdjustment();
        Apptitlebar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var customDragRegionPosition = Apptitlebar.TransformToVisual(null).TransformPoint(new Point(0, 0));

        var dragRectsList = new List<Windows.Graphics.RectInt32>();

        for (int i = 0; i < 2; i++)
        {
            var dragRect = new Windows.Graphics.RectInt32
            {
                X = (int)((customDragRegionPosition.X + (i * Apptitlebar.ActualWidth / 2)) * scaleAdjustment),
                Y = (int)(customDragRegionPosition.Y * scaleAdjustment),
                Height = (int)((Apptitlebar.ActualHeight - customDragRegionPosition.Y) * scaleAdjustment),
                Width = (int)((Apptitlebar.ActualWidth / 2) * scaleAdjustment)
            };

            dragRectsList.Add(dragRect);
        }

        var dragRects = dragRectsList.ToArray();

        if (appWindow.TitleBar != null)
        {
            appWindow.TitleBar.SetDragRectangles(dragRects);
        }
    }

    private int maxTabItems = 20;
    private async void Tabs_TabItemsChanged(TabView sender, IVectorChangedEventArgs args)
    {
        if (sender.TabItems.Count == 0)
        {
            Application.Current.Exit();
        }
        // If there is only one tab left, disable dragging and reordering of Tabs.
        else if (sender.TabItems.Count == 1)
        {
            sender.CanReorderTabs = false;
            sender.CanDragTabs = false;
        }
        else
        {
            sender.CanReorderTabs = true;
            sender.CanDragTabs = true;
        }
    }

    private Passer CreatePasser(object parameter = null)
    {
        Passer passer = new()
        {
            Tab = Tabs.SelectedItem as FireBrowserTabViewItem,
            TabView = Tabs,
            ViewModel = ViewModel,
            Param = parameter,
        };
        return passer;
    }

    public void SelectNewTab()
    {
        var tabToSelect = Tabs.TabItems.Count - 1;
        Tabs.SelectedIndex = tabToSelect;
    }

    public void FocusUrlBox(string text)
    {
        UrlBox.Text = text;
        UrlBox.Focus(FocusState.Programmatic);
    }
    public void NavigateToUrl(string uri)
    {
        if (TabContent.Content is WebContent webContent)
        {
            webContent.WebViewElement.CoreWebView2.Navigate(uri.ToString());
        }
        else
        {
            launchurl ??= uri;
            TabContent.Navigate(typeof(WebContent), CreatePasser(uri));
        }
    }
    private void UrlBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        string input = UrlBox.Text.ToString();
        string inputtype = UrlHelper.GetInputType(input);

        try
        {
            if (input.Contains("firebrowser://"))
            {
                switch (input)
                {
                    case "firebrowser://newtab":
                        Tabs.TabItems.Add(CreateNewTab(typeof(NewTab)));
                        SelectNewTab();
                        break;
                    case "firebrowser://modules":
                        Tabs.TabItems.Add(CreateNewTab(typeof(ModulesInstaller)));
                        SelectNewTab();
                        break;
                    default:
                        // default behavior
                        break;
                }
            }
            else if (inputtype == "url")
            {
                NavigateToUrl(input.Trim());
            }
            else if (inputtype == "urlNOProtocol")
            {
                NavigateToUrl("https://" + input.Trim());
            }
            else
            {
                string searchurl;
                if (SearchUrl == null) searchurl = "https://www.google.nl/search?q=";
                else
                {
                    searchurl = SearchUrl;
                }
                string query = searchurl + input;
                NavigateToUrl(query);
            }
        }
        catch (Exception ex)
        {
            // Handle the exception, log it, or display an error message.
            Debug.WriteLine("Error during navigation: " + ex.Message);
        }



    }

    #region cangochecks
    private bool CanGoBack()
    {
        ViewModel.CanGoBack = (bool)(TabContent?.Content is WebContent
            ? TabWebView?.CoreWebView2.CanGoBack
            : TabContent?.CanGoBack);

        return ViewModel.CanGoBack;
    }


    private bool CanGoForward()
    {
        ViewModel.CanGoForward = (bool)(TabContent?.Content is WebContent
            ? TabWebView?.CoreWebView2.CanGoForward
            : TabContent?.CanGoForward);
        return ViewModel.CanGoForward;
    }


    private void GoBack()
    {
        if (CanGoBack() && TabContent != null)
        {
            if (TabContent.Content is WebContent && TabWebView.CoreWebView2.CanGoBack) TabWebView.CoreWebView2.GoBack();
            else if (TabContent.CanGoBack) TabContent.GoBack();
            else ViewModel.CanGoBack = false;
        }
    }

    private void GoForward()
    {
        if (CanGoForward() && TabContent != null)
        {
            if (TabContent.Content is WebContent && TabWebView.CoreWebView2.CanGoForward) TabWebView.CoreWebView2.GoForward();
            else if (TabContent.CanGoForward) TabContent.GoForward();
            else ViewModel.CanGoForward = false;
        }
    }
    #endregion

    #region click

    private async void ToolbarButtonClick(object sender, RoutedEventArgs e)
    {
        Passer passer = new()
        {
            Tab = Tabs.SelectedItem as FireBrowserTabViewItem,
            TabView = Tabs,
            ViewModel = ViewModel
        };

        switch ((sender as Button).Tag)
        {
            case "Back":
                GoBack();
                break;
            case "Forward":
                GoForward();
                break;
            case "Refresh":
                if (TabContent.Content is WebContent) TabWebView.CoreWebView2.Reload();
                break;
            case "Home":
                if (TabContent.Content is WebContent)
                {
                    TabContent.Navigate(typeof(NewTab));
                    UrlBox.Text = "";
                }
                break;
            case "Translate":
                if (TabContent.Content is WebContent)
                {
                    string url = (TabContent.Content as WebContent).WebViewElement.CoreWebView2.Source.ToString();
                    (TabContent.Content as WebContent).WebViewElement.CoreWebView2.Navigate("https://translate.google.com/translate?hl&u=" + url);
                }
                break;
            case "QRCode":
                try
                {
                    if (TabContent.Content is WebContent)
                    {
                        //Create raw qr code data
                        QRCodeGenerator qrGenerator = new QRCodeGenerator();
                        QRCodeData qrCodeData = qrGenerator.CreateQrCode((TabContent.Content as WebContent).WebViewElement.CoreWebView2.Source.ToString(), QRCodeGenerator.ECCLevel.M);

                        //Create byte/raw bitmap qr code
                        BitmapByteQRCode qrCodeBmp = new BitmapByteQRCode(qrCodeData);
                        byte[] qrCodeImageBmp = qrCodeBmp.GetGraphic(20);
                        using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                        {
                            using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                            {
                                writer.WriteBytes(qrCodeImageBmp);
                                await writer.StoreAsync();
                            }
                            var image = new BitmapImage();
                            await image.SetSourceAsync(stream);

                            QRCodeImage.Source = image;
                        }
                    }
                    else
                    {
                        //await UI.ShowDialog("Information", "No Webcontent Detected ( Url )");
                        QRCodeFlyout.Hide();
                    }

                }
                catch
                {
                    //await UI.ShowDialog("Error", "An error occurred while trying to generate your qr code");
                    QRCodeFlyout.Hide();
                }
                break;
            case "ReadingMode":

                break;
            case "AdBlock":

                break;
            case "AddFavoriteFlyout":
                if (TabContent.Content is WebContent)
                {
                    FavoriteTitle.Text = TabWebView.CoreWebView2.DocumentTitle;
                    FavoriteUrl.Text = TabWebView.CoreWebView2.Source;
                }
                break;
            case "AddFavorite":
                FireBrowserMultiCore.User auth = AuthService.CurrentUser;
                FavManager fv = new FavManager();
                fv.SaveFav(auth, FavoriteTitle.Text.ToString(), FavoriteUrl.Text.ToString());
                break;
            case "Favorites":
                FireBrowserMultiCore.User user = AuthService.CurrentUser;
                FavManager fs = new FavManager();
                List<FavItem> favorites = fs.LoadFav(user);

                FavoritesListView.ItemsSource = favorites;
                break;
            case "DarkMode":
                if (TabContent.Content is WebContent)
                {

                }
                break;

            case "History":
                FetchBrowserHistory();
                break;
        }
    }



    #endregion

    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TabContent?.Content is WebContent webContent)
        {
            TabWebView.NavigationStarting += (s, e) =>
            {
                ViewModel.CanRefresh = false;
            };
            TabWebView.NavigationCompleted += (s, e) =>
            {
                ViewModel.CanRefresh = true;
            };
            await TabWebView.EnsureCoreWebView2Async();
            SmallUpdates();
        }
        else
        {
            ViewModel.CanRefresh = false;
            ViewModel.CurrentAddress = null;
        }
    }



    private async void TabMenuClick(object sender, RoutedEventArgs e)
    {
        switch ((sender as Button).Tag)
        {
            case "NewTab":
                Tabs.TabItems.Add(CreateNewTab(typeof(NewTab)));
                SelectNewTab();
                break;
            case "NewWindow":
                
                MainWindow newWindow = new();
                newWindow.Activate();
                break;
            case "Share":

                break;
            case "DevTools":
                if(TabContent.Content is WebContent)
                {
                    (TabContent.Content as WebContent).WebViewElement.CoreWebView2.OpenDevToolsWindow();                   
                }

                break;
            case "Settings":
                Tabs.TabItems.Add(CreateNewTab(typeof(SettingsPage)));
                SelectNewTab();
                break;
            case "FullScreen":
                if (isFull == true)
                {
                    GoFullScreen(false);
                   
                }
                else
                {
                    GoFullScreen(true);

                }

                break;
            case "Downloads":
                UrlBox.Text = "firebrowser://downloads";
                TabContent.Navigate(typeof(FireBrowserWinUi3.Pages.TimeLinePages.MainTimeLine));
                break;
            case "History":
                UrlBox.Text = "firebrowser://history";
                TabContent.Navigate(typeof(FireBrowserWinUi3.Pages.TimeLinePages.MainTimeLine));

                break;
            case "InPrivate":
                Tabs.TabItems.Add(CreateNewIncog(typeof(InPrivate)));
                break;
            case "Favorites":
                UrlBox.Text = "firebrowser://favorites";
                TabContent.Navigate(typeof(FireBrowserWinUi3.Pages.TimeLinePages.MainTimeLine));
                break;

        }
    }

    #region database

    private async void ClearDb()
    {
        FireBrowserMultiCore.User user = AuthService.CurrentUser;
        string username = user.Username;
        string databasePath = Path.Combine(
            UserDataManager.CoreFolderPath,
            UserDataManager.UsersFolderPath,
            username,
            "Database",
            "History.db"
        );

        HistoryTemp.ItemsSource = null;
        await DbClear.ClearTable(databasePath, "urls");
    }

    private ObservableCollection<HistoryItem> browserHistory;

    private async void FetchBrowserHistory()
    {
        FireBrowserMultiCore.User user = AuthService.CurrentUser;

        Batteries.Init();
        try
        {
            string username = user.Username;
            string databasePath = Path.Combine(
                UserDataManager.CoreFolderPath,
                UserDataManager.UsersFolderPath,
                username,
                "Database",
                "History.db"
            );

            if (File.Exists(databasePath))
            {
                using (var connection = new SqliteConnection($"Data Source={databasePath};"))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT url, title, visit_count, typed_count, hidden FROM urls ORDER BY id DESC";

                    using (SqliteCommand command = new SqliteCommand(sql, connection))
                    {
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            browserHistory = new ObservableCollection<HistoryItem>();

                            while (reader.Read())
                            {
                                HistoryItem historyItem = new HistoryItem
                                {
                                    Url = reader.GetString(0),
                                    Title = reader.IsDBNull(1) ? null : reader.GetString(1),
                                    VisitCount = reader.GetInt32(2),
                                    TypedCount = reader.GetInt32(3),
                                    Hidden = reader.GetInt32(4)
                                };

                                // Fetch the image source here
                                historyItem.ImageSource = new BitmapImage(new Uri("https://t3.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url=" + historyItem.Url + "&size=32"));

                                browserHistory.Add(historyItem);
                            }

                            // Bind the browser history items to the ListView
                            HistoryTemp.ItemsSource = browserHistory;
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine("Database file does not exist at the specified path.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
        }
    }
    #endregion

    public FireBrowserTabViewItem CreateNewIncog(Type page = null, object param = null, int index = -1)
    {
        if (index == -1) index = Tabs.TabItems.Count;


        UrlBox.Text = "";

        FireBrowserTabViewItem newItem = new()
        {
            Header = $"Incognito",
            IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource() { Symbol = Symbol.BlockContact }
        };

        Passer passer = new()
        {
            Tab = newItem,
            TabView = Tabs,
            ViewModel = new ToolbarViewModel(),
            Param = param
        };

        newItem.Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["FloatingTabViewItemStyle"];

        // The content of the tab is often a frame that contains a page, though it could be any UIElement.

        Frame frame = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 37, 0, 0)
        };

        if (page != null)
        {
            frame.Navigate(page, passer);
        }
        else
        {
            frame.Navigate(typeof(FireBrowserWinUi3.Pages.InPrivate), passer);
        }


        newItem.Content = frame;
        return newItem;
    }

    private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        TabViewItem selectedItem = args.Tab;
        var tabContent = (Frame)selectedItem.Content;

        if (tabContent.Content is WebContent webContent)
        {
            var webView = webContent.WebViewElement;

            if (webView != null)
            {
                webView.Close();
            }
        }

        var tabItems = (sender as TabView)?.TabItems;
        tabItems?.Remove(args.Tab);
    }

    private string selectedHistoryItem;
    private void Grid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // Get the selected HistoryItem object
        HistoryItem historyItem = ((FrameworkElement)sender).DataContext as HistoryItem;
        selectedHistoryItem = historyItem.Url;

        // Create a context menu flyout
        var flyout = new MenuFlyout();

        // Add a menu item for "Delete This Record" button
        var deleteMenuItem = new MenuFlyoutItem
        {
            Text = "Delete This Record",
        };

        // Set the icon for the menu item using the Unicode escape sequence
        deleteMenuItem.Icon = new FontIcon
        {
            Glyph = "\uE74D" // Replace this with the Unicode escape sequence for your desired icon
        };

        // Handle the click event directly within the right-tapped event handler
        deleteMenuItem.Click += (s, args) =>
        {
            FireBrowserMultiCore.User user = AuthService.CurrentUser;
            string username = user.Username;
            string databasePath = Path.Combine(
                UserDataManager.CoreFolderPath,
                UserDataManager.UsersFolderPath,
                username,
                "Database",
                "History.db"
            );
            // Perform the deletion logic here
            // Example: Delete data from the 'History' table where the 'Url' matches the selectedHistoryItem
            DbClearTableData db = new();
            db.DeleteTableData(databasePath, "urls", $"Url = '{selectedHistoryItem}'");
            if (HistoryTemp.ItemsSource is ObservableCollection<HistoryItem> historyItems)
            {
                var itemToRemove = historyItems.FirstOrDefault(item => item.Url == selectedHistoryItem);
                if (itemToRemove != null)
                {
                    historyItems.Remove(itemToRemove);
                }
            }
            // After deletion, you may want to update the UI or any other actions
        };

        flyout.Items.Add(deleteMenuItem);

        // Show the context menu flyout
        flyout.ShowAt((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender));
    }

    private void ClearHistoryDataMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ClearDb();
    }

    private void SearchHistoryMenuFlyout_Click(object sender, RoutedEventArgs e)
    {
        if (HistorySearchMenuItem.Visibility == Visibility.Collapsed)
        {
            HistorySearchMenuItem.Visibility = Visibility.Visible;
            HistorySmallTitle.Visibility = Visibility.Collapsed;
        }
        else
        {
            HistorySearchMenuItem.Visibility = Visibility.Collapsed;
            HistorySmallTitle.Visibility = Visibility.Visible;
        }
    }

    private void FilterBrowserHistory(string searchText)
    {
        if (browserHistory == null) return;

        // Clear the collection to start fresh with filtered items
        HistoryTemp.ItemsSource = null;

        // Filter the browser history based on the search text
        var filteredHistory = new ObservableCollection<HistoryItem>(browserHistory
            .Where(item => item.Url.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                           item.Title?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true));

        // Bind the filtered browser history items to the ListView
        HistoryTemp.ItemsSource = filteredHistory;
    }

    private void HistorySearchMenuItem_TextChanged(object sender, TextChangedEventArgs e)
    {
        string searchText = HistorySearchMenuItem.Text;
        FilterBrowserHistory(searchText);
    }

    private void FavoritesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ListView listView = sender as ListView;
        if (listView.ItemsSource != null)
        {
            // Get selected item
            FavItem item = (FavItem)listView.SelectedItem;
            string launchurlfav = item.Url;
            if (TabContent.Content is WebContent)
            {
                (TabContent.Content as WebContent).WebViewElement.CoreWebView2.Navigate(launchurlfav);
            }
            else
            {
                TabContent.Navigate(typeof(WebContent), CreatePasser(launchurlfav));
            }

        }
        listView.ItemsSource = null;
        FavoritesFly.Hide();
    }

    private void HistoryTemp_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ListView listView = sender as ListView;
        if (listView.ItemsSource != null)
        {
            // Get selected item
            HistoryItem item = (HistoryItem)listView.SelectedItem;
            string launchurlfav = item.Url;
            if (TabContent.Content is WebContent)
            {
                (TabContent.Content as WebContent).WebViewElement.CoreWebView2.Navigate(launchurlfav);
            }
            else
            {
                TabContent.Navigate(typeof(WebContent), CreatePasser(launchurlfav));
            }
        }
        listView.ItemsSource = null;
        HistoryFlyoutMenu.Hide();
    }

    private void DownBtn_Click(object sender, RoutedEventArgs e)
    {
        //FlyoutShowOptions options = new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Bottom };
        // DownloadFlyout.ShowAt(DownBtn, options);

        if (TabContent.Content is WebContent)
        {
            if (TabWebView.CoreWebView2.IsDefaultDownloadDialogOpen == true)
            {
                (TabContent.Content as WebContent).WebViewElement.CoreWebView2.CloseDefaultDownloadDialog();
            }
            else
            {
                (TabContent.Content as WebContent).WebViewElement.CoreWebView2.OpenDefaultDownloadDialog();
            }
        }
    }

    private void OpenHistoryMenuItem_Click(object sender, RoutedEventArgs e)
    {
        UrlBox.Text = "firebrowser://history";
        TabContent.Navigate(typeof(FireBrowserWinUi3.Pages.TimeLinePages.MainTimeLine));
    }

    private void OpenFavoritesMenu_Click(object sender, RoutedEventArgs e)
    {
        UrlBox.Text = "firebrowser://favorites";
        TabContent.Navigate(typeof(FireBrowserWinUi3.Pages.TimeLinePages.MainTimeLine));
    }

    private void MainUser_Click(object sender, RoutedEventArgs e)
    {
        if(UserFrame.Visibility == Visibility.Visible)
        {
            UserFrame.Visibility = Visibility.Collapsed;
        }
        else
        {
            UserFrame.Visibility = Visibility.Visible;
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        UserFrame.Visibility = Visibility.Collapsed;
    }
}
