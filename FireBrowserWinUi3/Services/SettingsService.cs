﻿using FireBrowserWinUi3.Services.Contracts;
using FireBrowserWinUi3DataCore.Actions;
using FireBrowserWinUi3Exceptions;
using FireBrowserWinUi3MultiCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FireBrowserWinUi3.Services
{
    public class SettingsService : ISettingsService
    {
        #region MemberProps
        public SettingsActions Actions { get; set; }
        public Settings CoreSettings { get; set; }
        #endregion

        public SettingsService()
        {
            Initialize();
        }

        public async void Initialize()
        {

            try
            {

                if (AuthService.IsUserAuthenticated)
                {
                    Actions = new SettingsActions(AuthService.CurrentUser.Username);
                    CoreSettings = await Actions?.GetSettingsAsync();
                }

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);

            }
        }

        public async Task SaveChangesToSettings(User user, FireBrowserWinUi3MultiCore.Settings settings)
        {
            try
            {
                if (!AuthService.IsUserAuthenticated) return;

                UserFolderManager.SaveUserSettings(AuthService.CurrentUser, settings);
                if (!File.Exists(Path.Combine(UserDataManager.CoreFolderPath, UserDataManager.UsersFolderPath, AuthService.CurrentUser.Username, "Settings", "Settings.db")))
                {
                    await Actions?.SettingsContext.Database.MigrateAsync();
                }

                await Actions?.UpdateSettingsAsync(settings);
                // get new from database. 
                CoreSettings = await Actions?.GetSettingsAsync();

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(ex);
                Console.WriteLine($"Error in Creating Settings Database: {ex.Message}");

            }

        }
    }
}