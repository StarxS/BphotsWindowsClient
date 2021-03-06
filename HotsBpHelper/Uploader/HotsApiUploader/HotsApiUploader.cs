﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Heroes.ReplayParser;
using HotsBpHelper.Api.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HotsBpHelper.Uploader
{
    public class HotsApiUploader : Uploader
    {
        const string ApiEndpoint = "http://hotsapi.net/api/v1";

        public bool UploadToHotslogs { get; set; } = true;
        
        /// <summary>
        /// Upload replay
        /// </summary>
        /// <param name="file"></param>
        public override async Task Upload(ReplayFile file)
        {
            file.HotsApiUploadStatus = UploadStatus.InProgress;
            if (file.Fingerprint != null && await CheckDuplicate(file.Fingerprint))
            {
                if (App.Debug)
                _log.Debug($"File {file} marked as duplicate");
                file.HotsApiUploadStatus = UploadStatus.Duplicate;
            }
            else
            {
                file.HotsApiUploadStatus = await Upload(file.Filename);
            }
        }

        /// <summary>
        /// Upload replay
        /// </summary>
        /// <param name="file">Path to file</param>
        /// <returns>Upload result</returns>
        private async Task<UploadStatus> Upload(string file)
        {
            try
            {
                string response;
                using (var client = new WebClient())
                {
                    var bytes =
                        await
                            client.UploadFileTaskAsync($"{ApiEndpoint}/upload?uploadToHotslogs={UploadToHotslogs}", file);
                    response = Encoding.UTF8.GetString(bytes);
                    var genericResponse = JsonConvert.DeserializeObject<GenericResponse>(response);
                    if (genericResponse.Success)
                    {
                        UploadStatus status;
                        
                        if (Enum.TryParse(genericResponse.Status, out status))
                        {
                            _log.Debug($"Uploaded file '{file}': {status}");
                            return status;
                        }

                        _log.Error($"Unknown upload status '{file}': {genericResponse.Status}");
                        return UploadStatus.UploadError;
                    }

                    _log.Warn($"Error uploading file '{file}': {response}");
                    return UploadStatus.UploadError;
                }
            }
            catch (WebException ex)
            {
                if (await CheckApiThrottling(ex.Response))
                {
                    return await Upload(file);
                }
                _log.Warn(ex, $"Error uploading file '{file}'");
                return UploadStatus.UploadError;
            }
        }

        /// <summary>
        /// Check replay fingerprint against database to detect duplicate
        /// </summary>
        /// <param name="fingerprint"></param>
        public async Task<bool> CheckDuplicate(string fingerprint)
        {
            try
            {
                string response;
                using (var client = new WebClient())
                {
                    response = await client.DownloadStringTaskAsync($"{ApiEndpoint}/replays/fingerprints/v3/{fingerprint}?uploadToHotslogs={UploadToHotslogs}");
                }
                var converted = JsonConvert.DeserializeObject<GenericResponse>(response);
                return converted.Exists;
            }

            catch (WebException ex)
            {
                if (await CheckApiThrottling(ex.Response))
                {
                    return await CheckDuplicate(fingerprint);
                }
                _log.Warn(ex, $"Error checking fingerprint '{fingerprint}'");
                return false;
            }
        }

        /// <summary>
        /// Mass check replay fingerprints against database to detect duplicates
        /// </summary>
        /// <param name="fingerprints"></param>
        public async Task<string[]> CheckDuplicate(IEnumerable<string> fingerprints)
        {
            try
            {
                string response;
                using (var client = new WebClient())
                {
                    response = await client.UploadStringTaskAsync($"{ApiEndpoint}/replays/fingerprints?uploadToHotslogs={UploadToHotslogs}", String.Join("\n", fingerprints));
                }
                dynamic json = JObject.Parse(response);
                return (json.exists as JArray).Select(x => x.ToString()).ToArray();
            }
            catch (WebException ex)
            {
                if (await CheckApiThrottling(ex.Response))
                {
                    return await CheckDuplicate(fingerprints);
                }
                _log.Warn(ex, $"Error checking fingerprint array");
                return new string[0];
            }
        }

        /// <summary>
        /// Mass check replay fingerprints against database to detect duplicates
        /// </summary>
        public async Task CheckDuplicate(IEnumerable<ReplayFile> replays)
        {
            var exists = new HashSet<string>(await CheckDuplicate(replays.Select(x => x.Fingerprint)));
            replays.Where(x => exists.Contains(x.Fingerprint)).Map(x => x.HotsApiUploadStatus = UploadStatus.Duplicate);
        }

        /// <summary>
        /// Get minimum HotS client build supported by HotsApi
        /// </summary>
        public async Task<int> GetMinimumBuild()
        {
            try
            {
                using (var client = new WebClient())
                {
                    var response = await client.DownloadStringTaskAsync($"{ApiEndpoint}/replays/min-build");
                    int build;
                    if (!int.TryParse(response, out build))
                    {
                        _log.Warn($"Error parsing minimum build: {response}");
                        return 0;
                    }
                    return build;
                }
            }
            catch (WebException ex)
            {
                if (await CheckApiThrottling(ex.Response))
                {
                    return await GetMinimumBuild();
                }
                _log.Warn(ex, $"Error getting minimum build");
                return 0;
            }
        }
    }
}
