﻿using CommunityToolkit.Mvvm.ComponentModel;
using FireBrowserBusinessCore.Models;

namespace FireBrowserCore.ViewModel;
public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private Settings.NewTabBackground backgroundType;
    [ObservableProperty]
    private string imageTitle;
    [ObservableProperty]
    private string imageCopyright;
    [ObservableProperty]
    private string imageCopyrightLink;
}