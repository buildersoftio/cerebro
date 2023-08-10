﻿using Cerebro.Core.IO.Services;
using Cerebro.Core.Models.Configurations;
using Cerebro.Core.Utilities.Consts;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Cerebro.Core.Services
{
    public class SystemRunnerService
    {
        private readonly ILogger<SystemRunnerService> _logger;
        private readonly IRootIOService _rootIOService;
        private readonly IConfigIOService _configIOService;
        private readonly IDataIOService _dataIOService;
        private readonly NodeConfiguration _nodeConfiguration;

        // from here we are changing the default state of the storage configuration
        private readonly StorageDefaultConfiguration _storageDefaultConfiguration;

        public SystemRunnerService(
            ILogger<SystemRunnerService> logger,
            IRootIOService rootIOService,
            IConfigIOService configIOService,
            IDataIOService dataIOService,
            NodeConfiguration nodeConfiguration,
            StorageDefaultConfiguration storageDefaultConfiguration)
        {
            _logger = logger;
            _rootIOService = rootIOService;
            _configIOService = configIOService;
            _dataIOService = dataIOService;
            _nodeConfiguration = nodeConfiguration;
            _storageDefaultConfiguration = storageDefaultConfiguration;

            Start();
        }

        public void Start()
        {
            Console.WriteLine("\r\n\r\n           ____                  _                 " + $"       Starting {SystemProperties.Name}" +
                "\r\n          / ___| ___  _ __  ___ | |__   _ __  ___  " + "       Set your information in motion." +
                "\r\n         | |    / _ \\| '__|/ _ \\| '_ \\ | '__|/ _ " +
                "\\ \r\n         | |___|  __/| |  |  __/| |_) || |  | (_) |" + $"       {SystemProperties.ShortName} {SystemProperties.Version}. Developed with love by Buildersoft LLC." +
                "\r\n          \\____|\\___||_|   \\___||_.__/ |_|   \\___/ " + $"       Licensed under the Apache License 2.0. See https://bit.ly/3DqVQbx" +
                "\r\n                                                   " + "       Cerebro is an open-source distributed streaming platform designed to deliver the best performance possible for high-performance data pipelines, streaming analytics, streaming between microservices and data integrations." +
                "\r\n");

            ExposePorts();

            Console.WriteLine("");
            Console.WriteLine($"                   Starting {SystemProperties.Name}...");
            Console.WriteLine("\n");

            CheckInitialStartingUp();
            CreateLoggingDirectory();

            _logger.LogInformation($"Starting {SystemProperties.Name}...");
            Console.WriteLine("");
            _logger.LogInformation($"Server environment:os.name: {GetOSName()}");
            _logger.LogInformation($"Server environment:os.platform: {Environment.OSVersion.Platform}");
            _logger.LogInformation($"Server environment:os.version: {Environment.OSVersion}");
            _logger.LogInformation($"Server environment:os.is64bit: {Environment.Is64BitOperatingSystem}");
            _logger.LogInformation($"Server environment:domain.user.name: {Environment.UserDomainName}");
            _logger.LogInformation($"Server environment:user.name: {Environment.UserName}");
            _logger.LogInformation($"Server environment:processor.count: {Environment.ProcessorCount}");
            _logger.LogInformation($"Server environment:dotnet.version: {Environment.Version}");
            Console.WriteLine("");

            _logger.LogInformation("Update settings");
            _logger.LogInformation($"Node identifier is '{_nodeConfiguration.NodeId}'");

            CheckRootDirectories();
            CheckConfigDirectories();

            UpdateStateOfDefaultConfiguration();

            _logger.LogInformation($"{SystemProperties.ShortName} is ready");

        }

        private void CreateLoggingDirectory()
        {
            if (_rootIOService.IsLogsRootDirectoryCreated() != true)
            {
                _logger.LogInformation("'logs' root directory is created");
                _rootIOService.CreateLogsRootDirectory();
            }
        }

        private void ExposePorts()
        {
            try
            {
                var exposedUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")!.Split(';');
                foreach (var url in exposedUrls)
                {
                    try
                    {
                        var u = new Uri(url);
                        if (u.Scheme == "https")
                            Console.WriteLine($"                   HTTPS Port exposed {u.Port} SSL");
                        else
                            Console.WriteLine($"                   HTTP  Port exposed {u.Port}");
                    }
                    catch (Exception)
                    {
                        if (url.StartsWith("https://"))
                            Console.WriteLine($"                   HTTPS Port exposed {url.Split(':').Last()} SSL");
                        else
                            Console.WriteLine($"                   HTTP  Port exposed {url.Split(':').Last()}");
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"                   Cerebro is running in IIS Server");
            }

        }
        private string GetOSName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }
            else if (RuntimeInformation.IsOSPlatform(osPlatform: OSPlatform.OSX))
            {
                return "MacOS";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Linux";
            }
            else
            {
                return "NOT_SUPPORTED";
            }
        }

        private void CheckRootDirectories()
        {
            if (_rootIOService.IsDataRootDirectoryCreated() != true)
            {
                _logger.LogInformation("Root directory [/data] is created");
                _rootIOService.CreateDataRootDirectory();
            }

            // create data/store directory
            if (_dataIOService.IsDataRootAddressesDirCreated() != true)
            {
                _logger.LogInformation("Root directory [/data/store] is created");
                _dataIOService.CreateDataRootAddressesDir();
            }

            if (_dataIOService.IsIndexesDirectoryCreated() != true)
            {
                _logger.LogInformation("Root directory [/data/store/indexes] is created");
                _dataIOService.CreateIndexesDirectory();
            }


            if (_rootIOService.IsConfigRootDirectoryCreated() != true)
            {
                _logger.LogInformation("Root directory [/config] is created");
                _rootIOService.CreateConfigRootDirectory();
            }

            if (_rootIOService.IsTempRootDirectoryCreated() != true)
            {
                _logger.LogInformation("Root directory [/logs] is created");
                _rootIOService.CreateTempRootDirectory();
            }
        }

        private void CheckConfigDirectories()
        {
            if (_configIOService.IsActiveDirectoryCreated() != true)
            {
                _logger.LogInformation("Directory [/config/active] is created");
                _configIOService.CreateActiveDirectory();

                _configIOService.CreateStorageDefaultActiveFile();
            }


        }

        private void UpdateStateOfDefaultConfiguration()
        {
            // updating the default storage configuration
            _storageDefaultConfiguration.UpdateStorageDefaultConfigs(_configIOService.GetStorageDefaultConfiguration()!);
        }

        private void CheckInitialStartingUp()
        {
            if (_rootIOService.IsInitialConfiguration() == true)
            {
                _logger.LogInformation("Doing initial configuration");
            }
        }
    }
}
