﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using PervasiveDigital.Scratch.Common;

namespace PervasiveDigital.Scratch.Common
{
    public class FirmwareManager
    {
        private const string S4NHost = "http://az787340.vo.msecnd.net/";
        private const string FirmwarePath = "firmware/";
        private const string FirmwareDictionaryFile = "firmware-{0}.{1}.json";

        private string _s4nPath;
        private string _fwPath;
        private string _fwDictPath;
        private string _fwDictFileName;

        private FirmwareDictionary _firmwareDictionary;
        private object _fwDictLock = new object();

        public FirmwareManager()
        {
            EnsureDirectoryStructure();
            Task.Run(() => UpdateFirmwareDictionary());
        }

        private void EnsureDirectoryStructure()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _s4nPath = Path.Combine(appDataPath, "ScratchForDotNet");
            if (!Directory.Exists(_s4nPath))
                Directory.CreateDirectory(_s4nPath);
            _fwPath = Path.Combine(_s4nPath, "firmware");
            if (!Directory.Exists(_fwPath))
                Directory.CreateDirectory(_fwPath);
            ReadFirmwareDictionary();
        }

        private async void UpdateFirmwareDictionary()
        {
            var destPath = GetFirmwareDictionaryPath();
            var uriString = S4NHost + FirmwarePath + GetFirmwareDictionaryFileName();
            var uri = new Uri(uriString);

            var lastWrite = File.GetLastWriteTime(destPath);
            // Missing files use a year of 1601 instead of DateTime.MinValue. Treat anything
            //   before 2015 as a missing file, because unless I start time traveling, that is
            //   an invalid file.
            if (lastWrite.Year < 2015)
            {
                try
                {
                    // We've never retrieved the file - got get a copy unconditionally
                    // Use a temp file because DownloadFile will create a zero-length file if there is an error retrieving the web content
                    var tempFile = Path.GetTempFileName();
                    var client = new WebClient();
                    client.DownloadFile(uri, tempFile);
                    File.Copy(tempFile, destPath, true);
                    ReadFirmwareDictionary();
                }
                catch (WebException wex)
                {
                    _firmwareDictionary = null;
                }
            }
            else
            {
                try
                {
                    // The file exists locally - get it only if it is newer on the server
                    var buffer = new byte[1024];
                    var comparisonTime = lastWrite.ToUniversalTime();
                    var req = (HttpWebRequest)WebRequest.Create(uri);
                    req.IfModifiedSince = comparisonTime;
                    using (var response = (HttpWebResponse)await req.GetResponseAsync())
                    {
                        using (var sr = new StreamReader(response.GetResponseStream()))
                        {
                            var body = sr.ReadToEnd();
                            File.WriteAllText(destPath, body);
                        }
                    }
                    ReadFirmwareDictionary();
                }
                catch (WebException wex)
                {
                    if (wex.Response!=null)
                    {
                        var status = ((HttpWebResponse)wex.Response).StatusCode;
                        if (status == HttpStatusCode.NotModified)
                        {
                            // no big deal - we have the latest copy
                        }
                        else
                        {
                            // error - file not updated
                        }
                    }
                    else
                    {
                        // error - file not updated
                    }
                }
            }
        }

        private void ReadFirmwareDictionary()
        {
            lock (_fwDictLock)
            {
                try
                {
                    var content = File.ReadAllText(GetFirmwareDictionaryPath());

                    _firmwareDictionary = JsonConvert.DeserializeObject<FirmwareDictionary>(content);
                }
                catch (FileNotFoundException fnfex)
                {
                    _firmwareDictionary = null;
                }
            }
        }

        private string GetFirmwareDictionaryFileName()
        {
            if (string.IsNullOrEmpty(_fwDictFileName))
            {
                var version = typeof(FirmwareManager).Assembly.GetName().Version;
                _fwDictFileName = string.Format(FirmwareDictionaryFile, version.Major, version.Minor);
            }
            return _fwDictFileName;
        }

        private string GetFirmwareDictionaryPath()
        {
            if (string.IsNullOrEmpty(_fwDictPath))
            {
                var filename = GetFirmwareDictionaryFileName();
                _fwDictPath = Path.Combine(_fwPath, filename);
            }
            return _fwDictPath;
        }
    }
}
