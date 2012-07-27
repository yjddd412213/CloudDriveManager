﻿//---------------------------------------------------------------------------------------------------------------------------- 
// <copyright company="Microsoft Corporation"> 
//  Copyright 2012 Microsoft Corporation 
// </copyright> 
// Licensed under the MICROSOFT LIMITED PUBLIC LICENSE version 1.1 (the "License");  
// You may not use this file except in compliance with the License.  
//--------------------------------------------------------------------------------------------------------------------------- 

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.IO;
using System.Diagnostics;
using Contoso.Logging;

namespace Contoso.Cloud
{
    public class CloudDriveManager
    {
        CloudStorageAccount _account;
        CloudDrive _cloudDrive = null;
        string _vhdName;
        char _driveLetter;
        int _cacheSize;
        LocalResource _cache;
        Logger _logger;

        const int DISK_SIZE = 20;

        public CloudDriveManager(CloudStorageAccount account, string vhdPathAndName, char driveLetter, LocalResource cache)
        {
            _account = account;
            _vhdName = vhdPathAndName;
            _driveLetter = driveLetter;
            _cacheSize = cache.MaximumSizeInMegabytes / 2;
            _cache = cache;
            _logger = new Logger(account);
        }

        public void Mount()
        {
            _logger.Log(string.Format("mounting drive {0}", _vhdName));
            //_cloudDrive = _account.CreateCloudDrive(_vhdName);

            var driveLetter = _cloudDrive.Mount(_cacheSize, DriveMountOptions.Force);
            _logger.Log(string.Format("mounted drive letter {0}", driveLetter));

            var remounted = VerifyDriveLetter();
        }


        public bool VerifyDriveLetter()
        {
            _logger.Log("verifying drive letter");
            bool rv = false;
            if (RoleEnvironment.IsEmulated)
            {
                _logger.Log("Can't change drive letter in emulator");
                //return;
            }

            try
            {
                DriveInfo d = new DriveInfo(_cloudDrive.LocalPath);
                if (string.IsNullOrEmpty(_cloudDrive.LocalPath))
                {
                    _logger.Log("verifydriveLetter: Not Mounted?");
                    throw new InvalidOperationException("drive is notmounted");
                }

                if (!char.IsLetter(_cloudDrive.LocalPath[0]))
                {
                    _logger.Log("verifiydriveLeter: Not a letter?");
                    throw new InvalidOperationException("verifydriveletter - not a letter?");
                }

                if (IsSameDrive())
                {
                    _logger.Log("is same drive; no need to diskpart...");
                    return true;
                }

                char mountedDriveLetter = CurrentLocalDrive(_vhdName);
                RunDiskPart(_driveLetter, mountedDriveLetter);

                if (!IsSameDrive())
                {
                    var msg = "Drive change failed to change";
                    _logger.Log(msg);
                    throw new ApplicationException(msg);
                }
                else
                {
                    Mount();
                }

                _logger.Log("verifydriveletter done!!");
                return rv;

            }
            catch (Exception ex)
            {
                _logger.Log("error verifydriveletter", ex);
                return rv;
            }

        }

        bool IsSameDrive()
        {
            char targetDrive = _driveLetter.ToString().ToLower()[0];
            char currentDrive = CurrentLocalDrive(_vhdName);

            string msg = string.Format(
                "target drive: {0} - current drive: {1}",
                targetDrive,
                currentDrive);

            _logger.Log(msg);

            if (targetDrive == currentDrive)
            {
                _logger.Log("verifydriveLetter: already same drive");
                return true;
            }
            else
                return false;

        }

        char CurrentLocalDrive(string vhdName)
        {
            KeyValuePair<string, Uri> currentDrivePath = new KeyValuePair<string, Uri>();
            foreach (var drive in CloudDrive.GetMountedDrives())
            {
                if (drive.Value.AbsolutePath.Contains(_vhdName))
                {
                    currentDrivePath = drive;
                    break;
                }
                throw new ApplicationException("drive can't be found in listed mounted drives");
            }

            char currentDrive = currentDrivePath.Key.ToLower()[0];
            return currentDrive;
        }

        void RunDiskPart(char destinationDriveLetter, char mountedDriveLetter)
        {
            string diskpartFile = Path.Combine(_cache.RootPath, "diskpart.txt");

            if (File.Exists(diskpartFile))
            {
                File.Delete(diskpartFile);
            }

            string cmd = "select volume = " + mountedDriveLetter + "\r\n" + "assign letter = " + destinationDriveLetter;
            File.WriteAllText(diskpartFile, cmd);

            //start the process
            _logger.Log("running diskpart now!!!!");
            _logger.Log("using " + cmd);
            using (Process changeletter = new Process())
            {
                changeletter.StartInfo.Arguments = "/s" + " " + diskpartFile;
                changeletter.StartInfo.FileName = System.Environment.GetEnvironmentVariable("WINDIR") + "\\System32\\diskpart.exe";
                //#if !DEBUG
                changeletter.Start();
                changeletter.WaitForExit();
                //#endif
            }

            File.Delete(diskpartFile);

        }

        public void CreateDrive()
        {
            try
            {
                _cloudDrive = _account.CreateCloudDrive(_vhdName);
                _logger.Log("using account " + _account.BlobEndpoint + " to create " + _vhdName);
                var rv = _cloudDrive.CreateIfNotExist(DISK_SIZE);
                _logger.Log("done create drive");
            }
            catch (Exception ex)
            {
                _logger.Log("error on CreateDrive", ex);
            }
        }


        public void CreateDriveEx()
        {
            try
            {
                CloudBlobClient client = _account.CreateCloudBlobClient();

                // Create the container for the drive if it does not already exist.
                CloudBlobContainer container = new CloudBlobContainer("mydrives", client);
                container.CreateIfNotExist();

                // Get a reference to the page blob that will back the drive.
                CloudPageBlob pageBlob = container.GetPageBlobReference(_vhdName);
                _cloudDrive = new CloudDrive(pageBlob.Uri, _account.Credentials);

                //_cloudDrive = _account.CreateCloudDrive(_vhdName);
                _logger.Log("using account " + _account.BlobEndpoint + " to create " + _vhdName);
                var rv = _cloudDrive.CreateIfNotExist(DISK_SIZE);
                _logger.Log("done create drive");
            }
            catch (Exception ex)
            {
                _logger.Log("error on CreateDrive", ex);
            }
        }
    }
}
