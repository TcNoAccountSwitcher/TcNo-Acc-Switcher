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

using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TcNo_Acc_Switcher_Server.State.Classes;

public class Navigation
{
    [Inject] private IJSRuntime JsRuntime { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }

    public event Action OnBackButtonClick;
    public event Action OnBackButtonReset;
    public void ClickBackButton() => OnBackButtonClick?.Invoke();
    public void ResetBackButton() => OnBackButtonReset?.Invoke();

    // Rather just use the reload code directly, for this to not be a circular DI (AppState -> Navigation -> Discord (or anything) -> AppState)
    //public void ReloadPage() => NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
    public void ReloadWithToast(string type, string title, string message) =>
        NavigationManager.NavigateTo($"{NavigationManager.BaseUri}?toast_type={type}&toast_title={Uri.EscapeDataString(title)}&toast_message={Uri.EscapeDataString(message)}");
    public void NavigateToWithToast(string uri, string type, string title, string message) =>
        NavigationManager.NavigateTo($"{uri}?toast_type={type}&toast_title={Uri.EscapeDataString(title)}&toast_message={Uri.EscapeDataString(message)}");
    public void NavigateTo(string uri, bool forceLoad = false) => NavigationManager.NavigateTo(uri, forceLoad);

    public void NavigateUpOne()
    {
        var uri = NavigationManager.Uri;
        if (uri.EndsWith('/')) uri = uri[..^1];
        uri = uri.Replace("http://", "").Replace("https://", "");

        // Navigate up one folder
        if (uri.Contains("/"))
        {
            var split = uri.Split('/');
            var newUri = "http://" + string.Join("/", split.Take(split.Length - 1));
            NavigationManager.NavigateTo(newUri);
        }
        else
        {
            ClickBackButton();
        }
    }
}