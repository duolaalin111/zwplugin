using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZwSoft.ZwCAD.ApplicationServices;

namespace ZrxDotNetCSProject5
{
    public static class CadHelper
    {
        public static async Task<bool> SendCommandAndWaitAsync(Document doc, string executeString, string commandName, int timeoutMs = 60000)
        {
            var tcs = new TaskCompletionSource<bool>();

            CommandEventHandler endedHandler = null;
            CommandEventHandler cancelledHandler = null;
            CommandEventHandler failedHandler = null;

            void CleanupEvents()
            {
                doc.CommandEnded -= endedHandler;
                doc.CommandCancelled -= cancelledHandler;
                doc.CommandFailed -= failedHandler;
            }

            endedHandler = (s, e) =>
            {
                if (e.GlobalCommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                {
                    CleanupEvents();
                    tcs.TrySetResult(true);
                }
            };

            cancelledHandler = (s, e) =>
            {
                if (e.GlobalCommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                {
                    CleanupEvents();
                    tcs.TrySetResult(false);
                }
            };

            failedHandler = (s, e) =>
            {
                if (e.GlobalCommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                {
                    CleanupEvents();
                    tcs.TrySetResult(false);
                }
            };

            doc.CommandEnded += endedHandler;
            doc.CommandCancelled += cancelledHandler;
            doc.CommandFailed += failedHandler;

            doc.SendStringToExecute(executeString, true, false, false);

            var timeoutTask = Task.Delay(timeoutMs);
            var finishedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (finishedTask == timeoutTask)
            {
                CleanupEvents();
                throw new Exception($"等待 CAD 命令执行超时 ({timeoutMs / 1000}秒)。");
            }

            return await tcs.Task;
        }

        public static async Task<string> GetDescPropForNode(HttpClient httpClient, long cabinetId)
        {
            var response = await httpClient.GetAsync($"api/drawings/simple?cabinetId={cabinetId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                if (root.GetProperty("code").GetInt32() != 200)
                    return null;

                if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                {
                    var firstItem = dataArray[0];
                    if (firstItem.TryGetProperty("attrNames", out var attrNamesElement)
                        && attrNamesElement.ValueKind == JsonValueKind.Array)
                    {
                        var attrNames = attrNamesElement.EnumerateArray()
                            .Select(x => x.GetString())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();

                        if (attrNames.Count == 0)
                            return "{}";

                        var properties = new Dictionary<string, string>();
                        foreach (var name in attrNames)
                            properties[name] = "";

                        return JsonSerializer.Serialize(properties);
                    }
                }

                return "{}";
            }
        }

        public static async Task DownloadFileAsync(HttpClient httpClient, string url, string localPath)
        {
            if (!url.StartsWith("http"))
                url = httpClient.BaseAddress + url.TrimStart('/');

            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (var streamToRead = await response.Content.ReadAsStreamAsync())
                using (var streamToWrite = File.Open(localPath, FileMode.Create))
                {
                    await streamToRead.CopyToAsync(streamToWrite);
                }
            }
        }
    }
}
