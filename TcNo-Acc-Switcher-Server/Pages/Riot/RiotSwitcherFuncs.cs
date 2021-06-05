﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TcNo_Acc_Switcher_Globals;
using TcNo_Acc_Switcher_Server.Data;
using TcNo_Acc_Switcher_Server.Pages.General;

namespace TcNo_Acc_Switcher_Server.Pages.Riot
{
    public class RiotSwitcherFuncs
    {
        private static readonly Data.Settings.Riot Riot = Data.Settings.Riot.Instance;
        /*
                private static string _riotRoaming = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Riot");
        */
        private static readonly string RiotLocalAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riot Games\\Riot Client");

        private static string _riotClientPrivateSettings = "";

        /// <summary>
        /// Main function for Riot Account Switcher. Run on load.
        /// Collects accounts from cache folder
        /// Prepares HTML Elements string for insertion into the account switcher GUI.
        /// </summary>
        /// <returns>Whether account loading is successful, or a path reset is needed (invalid dir saved)</returns>
        public static async void LoadProfiles()
        {
            // Normal:
            Globals.DebugWriteLine(@"[Func:Riot\RiotSwitcherFuncs.LoadProfiles] Loading Riot profiles");

            LoadImportantData(); // If not already loaded -- Likely it will be already
            if (_delayedToasts.Count > 0)
            {
                foreach (var delayedToast in _delayedToasts)
                {
                    _ = GeneralInvocableFuncs.ShowToast(delayedToast[0], delayedToast[1], delayedToast[2], "toastarea");
                }
            }

            var localCachePath = "LoginCache\\Riot\\";
            if (!Directory.Exists(localCachePath)) return;
            var accList = new List<string>();
            foreach (var f in Directory.GetDirectories(localCachePath))
            {
                var lastSlash = f.LastIndexOf("\\", StringComparison.Ordinal) + 1;
                var accName = f.Substring(lastSlash, f.Length - lastSlash);
                accList.Add(accName);
            }

            // Order
            if (File.Exists("LoginCache\\Riot\\order.json"))
            {
                var savedOrder = JsonConvert.DeserializeObject<List<string>>(await File.ReadAllTextAsync("LoginCache\\Riot\\order.json"));
                if (savedOrder != null)
                {
                    var index = 0;
                    if (savedOrder is { Count: > 0 })
                        foreach (var acc in from i in savedOrder where accList.Any(x => x == i) select accList.Single(x => x == i))
                        {
                            accList.Remove(acc);
                            accList.Insert(index, acc);
                            index++;
                        }
                }
            }

            foreach (var element in
                accList.Select(accName =>
                    $"<div class=\"acc_list_item\"><input type=\"radio\" id=\"{accName}\" Username=\"{accName}\" DisplayName=\"{accName}\" class=\"acc\" name=\"accounts\" onchange=\"SelectedItemChanged()\" />\r\n" +
                    $"<label for=\"{accName}\" class=\"acc\">\r\n" +
                    $"<img src=\"\\img\\profiles\\riot\\{accName.Replace("#", "-")}.jpg\" draggable=\"false\" />\r\n" +
                    $"<h6>{accName}</h6></div>\r\n"))
                await AppData.ActiveIJsRuntime.InvokeVoidAsync("jQueryAppend", "#acc_list", element);

            _ = AppData.ActiveIJsRuntime.InvokeVoidAsync("jQueryProcessAccListSize");
            await AppData.ActiveIJsRuntime.InvokeVoidAsync("initContextMenu");
            await AppData.ActiveIJsRuntime.InvokeVoidAsync("initAccListSortable");
            Riot.SaveSettings();
        }

        // Delayed toasts, as notifications are created in the LoadImportantData() section, and can be before the main process has rendered items.
        private static List<List<string>> _delayedToasts = new();

        /// <summary>
        /// Run necessary functions and load data when being launcher without a GUI (From command line for example).
        /// </summary>
        public static void LoadImportantData()
        {
            // Load once
            if (Riot.Initialised) return;

            _riotClientPrivateSettings = Path.Join(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riot Games\\Riot Client\\Data", "RiotClientPrivateSettings.yaml"));

            // Check what games are installed:
            var riotClientInstallsFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Riot Games\\RiotClientInstalls.json");
            if (!File.Exists(riotClientInstallsFile)) return;

            var o = JObject.Parse(File.ReadAllText(riotClientInstallsFile));
            if (!o.ContainsKey("associated_client")) return;

            var assocClient = (JObject)o["associated_client"];
            if (assocClient == null) return;
            Riot.LeagueDir = null;
            Riot.RuneterraDir = null;
            Riot.ValorantDir = null;
            foreach (var (key, value) in assocClient)
            {
                if (key.Contains("League"))
                {
                    if (Riot.LeagueDir != null)
                    {
                        _delayedToasts.Add(new List<string>(){ "error", "More than 1 League install found", "Duplicate Install" });
                        continue;
                    }
                    Riot.LeagueDir = key.Replace('/', '\\');
                    Riot.LeagueRiotDir = ((string)value)?.Replace('/', '\\');
                }
                else if (key.Contains("LoR"))
                {
                    if (Riot.RuneterraDir != null)
                    {
                        _delayedToasts.Add(new List<string>() { "error", "More than 1 Runeterra install found", "Duplicate Install" });
                        continue;
                    }
                    Riot.RuneterraDir = key.Replace('/', '\\');
                    Riot.RuneterraRiotDir = ((string)value)?.Replace('/', '\\');
                }
                else if (key.Contains("VALORANT"))
                {
                    if (Riot.ValorantDir != null)
                    {
                        _delayedToasts.Add(new List<string>() { "error", "More than 1 VALORANT install found", "Duplicate Install" });
                        continue;
                    }
                    Riot.ValorantDir = key.Replace('/', '\\');
                    Riot.ValorantRiotDir = ((string)value)?.Replace('/', '\\');
                }
            }

            Riot.Initialised = true;
        }

        /// <summary>
        /// Used in JS. Gets whether forget account is enabled (Whether to NOT show prompt, or show it).
        /// </summary>
        /// <returns></returns>
        [JSInvokable]
        public static Task<bool> GetRiotForgetAcc() => Task.FromResult(Riot.ForgetAccountEnabled);

        /// <summary>
        /// Remove requested account from loginusers.vdf
        /// </summary>
        /// <param name="accName">Riot account name</param>
        public static bool ForgetAccount(string accName)
        {
            Globals.DebugWriteLine($@"[Func:RiotRiotSwitcherFuncs.ForgetAccount] Forgetting account: hidden");
            // Remove image
            var img = Path.Join(GeneralFuncs.WwwRoot, $"\\img\\profiles\\riot\\{accName.Replace("#", "-")}.jpg");
            if (File.Exists(img)) File.Delete(img);
            // Remove cached files
            GeneralFuncs.RecursiveDelete(new DirectoryInfo($"LoginCache\\Riot\\{accName}"), false);
            // Remove from Tray
            Globals.RemoveTrayUser("Riot", accName); // Add to Tray list
            return true;
        }

        /// <summary>
        /// Restart Riot with a new account selected. Leave args empty to log into a new account.
        /// </summary>
        /// <param name="accName">(Optional) User's login username</param>
        public static void SwapRiotAccounts(string accName = "")
        {
            Globals.DebugWriteLine($@"[Func:Riot\RiotSwitcherFuncs.SwapRiotAccounts] Swapping to: hidden.");
            AppData.ActiveIJsRuntime.InvokeVoidAsync("updateStatus", "Closing Riot");
            if (!CloseRiot()) return;
            // DO ACTUAL SWITCHING HERE
            ClearCurrentLoginRiot();
            if (accName != "")
            {
                RiotCopyInAccount(accName);
                Globals.AddTrayUser("Riot", "+r:" + accName, accName, Riot.TrayAccNumber); // Add to Tray list
                _ = GeneralInvocableFuncs.ShowToast("success", "Changed user. Start a game below.", "Success", "toastarea");
            }

            //GeneralFuncs.StartProgram(Riot.Exe(), Riot.Admin);

            AppData.ActiveIJsRuntime.InvokeVoidAsync("updateStatus", "Ready");
            Globals.RefreshTrayArea();
        }
        
        private static void ClearCurrentLoginRiot()
        {
            Globals.DebugWriteLine(@"[Func:Riot\RiotSwitcherFuncs.ClearCurrentLoginRiot]");
            if (File.Exists(_riotClientPrivateSettings)) File.Delete(_riotClientPrivateSettings);
        }

        private static void RiotCopyInAccount(string accName)
        {
            Globals.DebugWriteLine(@"[Func:Riot\RiotSwitcherFuncs.RiotCopyInAccount]");
            LoadImportantData();
            var localCachePath = Path.Join($"LoginCache\\Riot\\{accName}\\", "RiotClientPrivateSettings.yaml");

            File.Copy(localCachePath, _riotClientPrivateSettings, true);
        }
        
        public static void RiotAddCurrent(string accName)
        {
            Globals.DebugWriteLine(@"[Func:Riot\RiotSwitcherFuncs.RiotAddCurrent]");
            var localCachePath = $"LoginCache\\Riot\\{accName}\\";
            Directory.CreateDirectory(localCachePath);

            if (!File.Exists(_riotClientPrivateSettings))
            {
                _ = GeneralInvocableFuncs.ShowToast("error", "Could not locate logged in user", "Failed", "toastarea");
                return;
            }
            // Save files
            File.Copy(_riotClientPrivateSettings, Path.Join(localCachePath, "RiotClientPrivateSettings.yaml"), true);
            
            // Copy in profile image from default
            Directory.CreateDirectory(Path.Join(GeneralFuncs.WwwRoot, "\\img\\profiles\\riot"));
            File.Copy(Path.Join(GeneralFuncs.WwwRoot, "\\img\\RiotDefault.png"), Path.Join(GeneralFuncs.WwwRoot, $"\\img\\profiles\\riot\\{accName.Replace("#", "-")}.jpg"), true);

            AppData.ActiveNavMan?.NavigateTo("/Riot/?cacheReload&toast_type=success&toast_title=Success&toast_message=" + Uri.EscapeUriString("Saved: " + accName), true);
        }

        public static void ChangeUsername(string oldName, string newName, bool reload = true)
        {
            File.Move(Path.Join(GeneralFuncs.WwwRoot, $"\\img\\profiles\\riot\\{Uri.EscapeUriString(oldName).Replace("#", "-")}.jpg"),
                Path.Join(GeneralFuncs.WwwRoot, $"\\img\\profiles\\riot\\{Uri.EscapeUriString(newName).Replace("#", "-")}.jpg")); // Rename image
            Directory.Move($"LoginCache\\Riot\\{oldName}\\", $"LoginCache\\Riot\\{newName}\\"); // Rename login cache folder

            if (reload) AppData.ActiveNavMan?.NavigateTo("/Riot/?cacheReload&toast_type=success&toast_title=Success&toast_message=" + Uri.EscapeUriString("Changed username"), true);
        }

        #region RIOT_MANAGEMENT
        /// <summary>
        /// List of Riot processes to close
        /// </summary>
        private static readonly string[] RiotProcessList = { "LeagueClient.exe", "LoR.exe", "VALORANT.exe", "RiotClientServices.exe", "RiotClientUx.exe", "RiotClientUxRender.exe" };
        
        /// <summary>
        /// Returns true if program can kill all Riot processes
        /// </summary>
        public static bool CanCloseRiot() => RiotProcessList.Aggregate(true, (current, s) => current & GeneralFuncs.CanKillProcess(s));

        /// <summary>
        /// Kills Riot processes when run via cmd.exe
        /// </summary>
        public static bool CloseRiot()
        {
            Globals.DebugWriteLine(@"[Func:Riot\RiotSwitcherFuncs.CloseRiot]");
            if (!CanCloseRiot()) return false;

            // Kill game clients & Platform clients
            foreach (var s in RiotProcessList)
            {
                Globals.KillProcess(s);
            }
            return true;
        }

        /// <summary>
        /// Start Riot games
        /// </summary>
        /// <param name="game"></param>
        public static void RiotStart(char game)
        {
            string name = "", dir = "", args = "";
            switch (game)
            {
                case 'l':
                    dir = Riot.LeagueRiotDir;
                    args = "--launch-product=league_of_legends --launch-patchline=live";
                    name = "League of Legends";
                    break;
                case 'r':
                    dir = Riot.RuneterraRiotDir;
                    args = "--launch-product=bacon --launch-patchline=live";
                    name = "Legends of Runeterra";
                    break;
                case 'v':
                    dir = Riot.ValorantRiotDir;
                    args = "--launch-product=valorant --launch-patchline=live";
                    name = "Valorant";
                    break;
            }

            var proc = new Process
            {
                StartInfo =
                {
                    FileName = dir,
                    Arguments = args,
                    UseShellExecute = true,
                    Verb = (Riot.Admin ? "runas" : "")
                }
            };
            proc.Start();

            _ = GeneralInvocableFuncs.ShowToast("info", "Started " + name, "Success", "toastarea");
        }

        /// <summary>
        /// Open Riot Games game folders
        /// </summary>
        /// <param name="game"></param>
        public static void RiotOpenFolder(char game)
        {
            var dir = game switch
            {
                'l' => Riot.LeagueDir,
                'r' => Riot.RuneterraDir,
                'v' => Riot.ValorantDir,
                _ => ""
            };

            Process.Start("explorer.exe", dir.Replace("/", "\\"));
        }
        #endregion
    }
}
