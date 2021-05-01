using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FTTLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace cdn
{
    class Program
    {
        private static string containerName => Config("containerName");
        private static string containerUrl => Config("containerUrl");
        private static string storageAccountConnectionString => Config("storageAccountConnectionString");
        private static string fileList => Path.GetFullPath(Config("fileList"));
        private static string[] extensions => Config("extensions", ".css,.eot,.gif,.jpeg,.jpg,.js,.otf,.png,.svg,.ttf,.woff,.woff2,.xsl").Split(',');
        private static Dictionary<string, string> files;

        private static int skipped = 0;
        private static int uploaded = 0;
        static void Main(string[] args)
        {
            try
            {
                ReadArgs(args);
                if (Validate())
                {
                    UploadFiles().Wait();
                    SaveFileList();
                    PrintSummary();
                }
                else
                {
                    log("Error: One or more configuration options were missing");
                    Environment.ExitCode = -1;
                }
            }
            catch (Exception e)
            {
                log(e.Message);
                log(e.StackTrace);
                Environment.ExitCode = 1;
            }
        }

        private static void PrintSummary()
        {
            foreach (var entry in files)
            {
                if (entry.Value.IsEmpty())
                {
                    log($"Warning: File not found: {entry.Key}");
                }
            }
            var max = $"{uploaded + skipped}".Length + 17;
            log($"{uploaded} file(s) uploaded".PadLeft(max));
            log($"{skipped} file(s) skipped".PadLeft(max));
        }

        private static void SaveFileList()
        {
            File.WriteAllText(fileList, JsonConvert.SerializeObject(files, Formatting.Indented));
        }

        private static bool Validate()
        {
            var valid = true;
            if (containerName.IsEmpty())
            {
                log("containerName is missing");
                valid = false;
            }
            if (containerUrl.IsEmpty())
            {
                log("containerUrl is missing");
                valid = false;
            }
            if (fileList.IsEmpty())
            {
                log("fileList is missing");
                valid = false;
            }
            if (storageAccountConnectionString.IsEmpty())
            {
                log("storageAccountConnectionString is missing");
                valid = false;
            }
            if (!fileList.FileExists())
            {
                log($"{fileList} was not found");
                valid = false;
            }
            return valid;
        }

        private static async Task UploadFiles()
        {
            files = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(fileList));
            // Create a BlobServiceClient object which will be used to create a container client
            BlobServiceClient blobServiceClient = new BlobServiceClient(storageAccountConnectionString);

            // Create the container and return a container client object
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var folder = Path.GetDirectoryName(fileList);
            var root = Path.GetDirectoryName(folder);
            var dir = new DirectoryInfo(folder);
            foreach (var file in dir.GetFilesWithExtensions(SearchOption.AllDirectories, extensions))
            {
                if (file.FullName != fileList)
                {
                    await Upload(containerClient, root, file);
                }
            }
        }

        private static async Task Upload(BlobContainerClient containerClient, string root, FileInfo file)
        {
            var blobName = file.BlobName(root);
            var key = "/" + blobName;
            var mutate = files.ContainsKey(key);
            using (var stream = file.OpenRead())
            {
                if (mutate)
                {
                    blobName = blobName.FingerPrintName(stream);
                }
            }

            //var info = await containerClient.UploadBlobAsync(blobName, stream);
            BlobClient blobClient = new BlobClient(storageAccountConnectionString, containerName, blobName);
            if (!blobClient.Exists())
            {
                log($"Uploading {key} to {blobName}");
                await blobClient.UploadAsync(file.OpenRead(), new BlobHttpHeaders { ContentType = FTT.GetMimeType(blobName) });
                uploaded++;
            }
            else
            {
                skipped++;
            }

            if (mutate)
            {
                files[key] = $"{containerUrl}{blobName}";
            }
        }

        #region Boilerplate code

        private static string startupDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private static Dictionary<string, string> configValues = new Dictionary<string, string>();
        private static Dictionary<string, string> _appSettings = null;
        private static Dictionary<string, string> AppSettings => _appSettings ?? GetAppSettings();

        private static Dictionary<string, string> GetAppSettings()
        {
            return
            _appSettings = "cdn.json".FileExists() ? JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText(
                    Path.Combine(startupDirectory, "cdn.json"))) : new Dictionary<string, string>();
        }

        private static string Config(string name, string defaultValue = null)
        {
            if (configValues != null && configValues.ContainsKey(name))
            {
                return configValues[name];
            }
            if (AppSettings.ContainsKey(name))
            {
                return AppSettings[name];
            }
            return ConfigurationManager.AppSettings[name] ?? Env(name, defaultValue);
        }

        private static string Env(string name, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return value.IsEmpty() ? defaultValue : value;
        }

        private static void ReadArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var key = args[i].Replace("-", "");
                i++;
                var value = args[i];
                configValues[key] = value;
            }
        }

        private static void log(string message)
        {
            Console.WriteLine(message);
        }
        #endregion

    }
}
