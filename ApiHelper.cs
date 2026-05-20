using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZrxDotNetCSProject5.newModels;

namespace ZrxDotNetCSProject5
{
    // 后端响应包装类
    public class ApiResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    // 搜索条件模型（添加 JsonPropertyName 确保字段名匹配）
    public class ProjectSearchCriteria
    {
        [JsonPropertyName("projectName")]
        public string projectName { get; set; }

        [JsonPropertyName("businessCode")]
        public string businessCode { get; set; }

        [JsonPropertyName("wbs")]
        public string wbs { get; set; }

        [JsonPropertyName("createType")]
        public string createType { get; set; }

        [JsonPropertyName("createBy")]
        public string createBy { get; set; }
    }

    public static class ApiHelper
    {
        private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("http://192.168.1.108:8080/api/") };

        // 获取项目列表
        public static async Task<List<Projectmodel>> GetProjectsAsync()
        {
            var response = await client.GetAsync("projects");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<Projectmodel>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (wrapper?.Code == 200 && wrapper.Data != null)
                return wrapper.Data;

            return null;
        }

        // 删除项目
        public static async Task<bool> DeleteProjectAsync(long id)
        {
            var response = await client.DeleteAsync($"projects/delete?id={id}");
            return response.IsSuccessStatusCode;
        }

        // 搜索项目
        public static async Task<List<Projectmodel>> SearchProjectsAsync(ProjectSearchCriteria criteria)
        {
            var json = JsonSerializer.Serialize(criteria);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("projects/search", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var resultJson = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<Projectmodel>>>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (wrapper?.Code == 200 && wrapper.Data != null)
                return wrapper.Data;

            return null;
        }
    }
}