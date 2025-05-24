using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PC3.Models;
using Newtonsoft.Json;

namespace PC3.Services;

public class JsonPlaceholderService : IJsonPlaceholderService
{
  private readonly HttpClient _httpClient;

  public JsonPlaceholderService(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public async Task<List<Post>?> GetPostsAsync()
  {
    try
    {

      var requestUrl = _httpClient.BaseAddress + "posts";
      Console.WriteLine($"Solicitando datos de: {requestUrl}");


      var response = await _httpClient.GetAsync("posts");


      Console.WriteLine($"Código de estado: {response.StatusCode}");

      if (!response.IsSuccessStatusCode)
      {
        Console.WriteLine($"Error en la solicitud: {response.StatusCode}");
        return null;
      }


      var content = await response.Content.ReadAsStringAsync();
      Console.WriteLine($"Datos recibidos (primeros 200 caracteres): {content[..Math.Min(200, content.Length)]}...");


      var posts = JsonConvert.DeserializeObject<List<Post>>(content);
      Console.WriteLine($"Número de posts deserializados: {posts?.Count ?? 0}");

      return posts;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Excepción al obtener posts: {ex.Message}");
      return null;
    }
  }

  public async Task<Post?> GetPostAsync(int id)
  {
    try
    {
      var response = await _httpClient.GetAsync($"posts/{id}");
      if (!response.IsSuccessStatusCode)
      {
        return null;
      }
      var content = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<Post>(content);
    }
    catch
    {
      return null;
    }
  }

  public async Task<List<Comment>?> GetCommentsForPostAsync(int postId)
  {
    try
    {
      var response = await _httpClient.GetAsync($"posts/{postId}/comments");
      if (!response.IsSuccessStatusCode)
      {
        return null;
      }
      var content = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<List<Comment>>(content);
    }
    catch
    {
      return null;
    }
  }

  public async Task<User?> GetUserAsync(int userId)
  {
    try
    {
      var response = await _httpClient.GetAsync($"users/{userId}");
      if (!response.IsSuccessStatusCode)
      {
        return null;
      }

      var content = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<User>(content);
    }
    catch
    {
      return null;
    }
  }

  public async Task<bool> ValidarPostExiste(int postId)
  {
    var post = await GetPostAsync(postId);
    return post != null;
  }
}