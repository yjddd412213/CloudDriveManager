using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Contoso.Cloud;
using Contoso.Logging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace Contoso.Cloud
{
    public class CloudDriveProvider
    {
        const string DCACHE_NAME = "DriveCacheName";
        const string DCACHE_LOCATION = "cache";
        const string STORAGE_ACCOUNT_SETTING = "DataConnectionString";
        const string DRIVE_SETTINGS = "DriveSettings";

        CloudDriveManager _cloudDriveManager = null;
        Logger _logger;

        public void CheckDrives()
        {
            _cloudDriveManager.VerifyDriveLetter();
        }


        public bool Start()
        {
            try
            {
                Initialize();
                MountAllDrives();
            }
            catch (Exception ex)
            {
                _logger.Log("fail on onstart", ex);
            }
            return true;
        }

        void MountAllDrives()
        {
            try
            {
                var driveSettings = RoleEnvironment.GetConfigurationSettingValue(DRIVE_SETTINGS);
                string[] settings = driveSettings.Split(':');
                CloudStorageAccount account = CloudStorageAccount.FromConfigurationSetting(STORAGE_ACCOUNT_SETTING);
                string dCacheName = RoleEnvironment.GetConfigurationSettingValue(DCACHE_NAME);
                LocalResource cache = RoleEnvironment.GetLocalResource(dCacheName);
                int cacheSize = cache.MaximumSizeInMegabytes / 2;
                _cloudDriveManager = new CloudDriveManager(account, settings[0], settings[1][0], cache);
                _cloudDriveManager.CreateDriveEx();
                _cloudDriveManager.Mount();
            }
            catch (Exception ex)
            {
                _logger.Log("fail on mountalldrives", ex);
                throw;
            }
        }

        /// <summary>
        /// Cloud Initialization area
        /// </summary>
        void Initialize()
        {
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
            {
                configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));
            });


            string dCacheName = RoleEnvironment.GetConfigurationSettingValue(DCACHE_NAME);
            LocalResource cache = RoleEnvironment.GetLocalResource(dCacheName);
            CloudDrive.InitializeCache(cache.RootPath + DCACHE_LOCATION, cache.MaximumSizeInMegabytes);

            var storageAccount = CloudStorageAccount.FromConfigurationSetting(STORAGE_ACCOUNT_SETTING);
            _logger = new Logger(storageAccount);


        }

        //public void Run()
        //{
        //    var storageAccount = CloudStorageAccount.FromConfigurationSetting(STORAGE_ACCOUNT_SETTING);

        //    while (true)
        //    {
        //        try
        //        {
        //            Thread.Sleep(TimeSpan.FromSeconds(30));
        //            CheckDrives();
        //            Trace.WriteLine("Working - from Cloud Drive Manager", "Information");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.Log("fail in Run", ex);
        //        }
        //    }

        //}
    }
}
