using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using Newtonsoft.Json;

namespace ConsoleApp3
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        public ApiClient()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7173/api") };
        }
        public async Task<ToDo> AddTask(ToDo newTask)
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/ToDo", newTask);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ToDo>();
        }
        public async Task<List<ToDo>> GetTasks()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("api/ToDo");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ToDo>>();
        }

        public async Task<ToDo> GetTaskById(int id)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"api/ToDo/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ToDo>();
        }

        public async Task UpdateTask(int id, ToDo updatedTask)
        {
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"api/ToDo/{id}", updatedTask);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateTaskStatus(int id, ToDo updatedTaskStatus)
        {
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"api/ToDo/{id}/complete", updatedTaskStatus);
        }


        public async Task DeleteTaskById(int id)
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync($"api/ToDo/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}



