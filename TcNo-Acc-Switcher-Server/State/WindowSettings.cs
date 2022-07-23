﻿// TcNo Account Switcher - A Super fast account switcher
// Copyright (C) 2019-2022 TechNobo (Wesley Pyburn)
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using TcNo_Acc_Switcher_Globals;
using TcNo_Acc_Switcher_Server.State.Classes;
using TcNo_Acc_Switcher_Server.State.Interfaces;

namespace TcNo_Acc_Switcher_Server.State;

public class WindowSettings : IWindowSettings, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }


    public string Language
    {
        get => _language;
        set => SetField(ref _language, value);
    }

    public bool Rtl
    {
        get => _rtl;
        set => SetField(ref _rtl, value);
    }

    public bool StreamerModeEnabled
    {
        get => _streamerModeEnabled;
        set => SetField(ref _streamerModeEnabled, value);
    }

    public int ServerPort { get; set; } = 0;
    public Point WindowSize { get; set; } = new() { X = 800, Y = 450 };
    public bool AllowTransparency { get; set; } = true;
    public string Version { get; set; } = Globals.Version;
    public List<object> DisabledPlatforms { get; } = new();
    public bool TrayMinimizeNotExit { get; set; } = false;
    public bool ShownMinimizedNotification { get; set; } = false;
    public bool StartCentered { get; set; } = false;

    public string ActiveTheme
    {
        get => _activeTheme;
        set => SetField(ref _activeTheme, value);
    }

    public string ActiveBrowser { get; set; } = "WebView";

    public string Background
    {
        get => _background;
        set => SetField(ref _background, value);
    }

    public List<string> EnabledBasicPlatforms { get; } = new();

    public bool CollectStats
    {
        get => _collectStats;
        set
        {
            SetField(ref _collectStats, value);
            if (!value) ShareAnonymousStats = false;
        }
    }

    public bool ShareAnonymousStats { get; set; } = true;
    public bool MinimizeOnSwitch { get; set; } = false;
    private bool _discordRpcEnabled = true;
    private string _language = "";
    private bool _rtl = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
    private bool _streamerModeEnabled = true;
    private string _activeTheme = "Dracula_Cyan";
    private string _background = "";
    private bool _collectStats = true;

    public bool DiscordRpcEnabled
    {
        get => _discordRpcEnabled;
        set
        {
            SetField(ref _discordRpcEnabled, value);
            if (!value) DiscordRpcShareTotalSwitches = false;
        }
    }

    public bool DiscordRpcShareTotalSwitches { get; set; } = true;
    public string PasswordHash { get; set; } = "";
    /// <summary>
    /// For BasicStats // Game statistics collection and showing
    /// Keys for metrics on this list are not shown for any account.
    /// List of all games:[Settings:Hidden metric] metric keys.
    /// </summary>
    public Dictionary<string, Dictionary<string, bool>> GloballyHiddenMetrics { get; set; } = new();
    public bool AlwaysAdmin { get; set; } = false;


    public ObservableCollection<PlatformItem> Platforms { get; set; } = new()
    {
        new PlatformItem("Discord", true),
        new PlatformItem("Epic Games", true),
        new PlatformItem("Origin", true),
        new PlatformItem("Riot Games", true),
        new PlatformItem("Steam", true),
        new PlatformItem("Ubisoft", true),
    };

    private static string Filename = "WindowSettings.json";
    /// <summary>
    /// Loads WindowSettings from file, if exists. Otherwise default.
    /// A toast for errors can not be displayed here as this needs to be loaded before the language instance.
    /// </summary>
    public WindowSettings()
    {
        Globals.LoadSettings(Filename, this, false);

        // TODO: Load from file? See commented at the bottom of this class. They were leftover from the Data\AppSettings file.
        Platforms.CollectionChanged += (_, _) => Platforms.Sort();
    }
    public void Save() => Globals.SaveJsonFile(Filename, this, false);


    /// <summary>
    /// Get platform details from an identifier, or the name.
    /// </summary>
    public PlatformItem GetPlatform(string nameOrId) => Platforms.FirstOrDefault(x => x.Name == nameOrId || x.PossibleIdentifiers.Contains(nameOrId));


    //private static void InitPlatformsList()
    //{
    //    // Add platforms, if none there.
    //    if (_platforms.Count == 0)
    //        _platforms = DefaultPlatforms;

    //    _platforms.First(x => x.Name == "Steam").SetFromPlatformItem(new PlatformItem("Steam", new List<string> { "s", "steam" }, "steam.exe", true));

    //    // Load other platforms by initializing BasicPlatforms
    //    _ = BasicPlatforms.Instance;
    //}

    //public class GameSetting
    //{
    //    public string SettingId { get; set; } = "";
    //    public bool Checked { get; set; }
    //}
}