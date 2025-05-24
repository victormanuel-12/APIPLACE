using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PC3.Models;
using PC3.Services;
namespace PC3.Controllers
{


  public class PostsController : Controller
  {
    private readonly IJsonPlaceholderService _jsonService;
    private readonly ILogger<PostsController> _logger;

    public PostsController(
        IJsonPlaceholderService jsonService,
        ILogger<PostsController> logger)
    {
      _jsonService = jsonService;
      _logger = logger;

    }


    public async Task<IActionResult> Index()
    {
      var posts = await _jsonService.GetPostsAsync();
      return View(posts);
    }

    public async Task<IActionResult> Details(int id)
    {
      _logger.LogInformation($"Obteniendo post con id: {id}");

      try
      {
        var post = await _jsonService.GetPostAsync(id);
        if (post == null)
        {
          _logger.LogWarning($"No se encontró el post con id: {id}");
          return NotFound();
        }

        var user = await _jsonService.GetUserAsync(post.userId);
        if (user == null)
        {
          _logger.LogWarning($"No se encontró el usuario con id: {post.userId}");
          return NotFound();
        }

        var comments = await _jsonService.GetCommentsForPostAsync(id) ?? new List<Comment>();
        _logger.LogInformation($"Encontrados {comments.Count} comentarios para post {id}");

        // Pasar datos a la vista
        ViewBag.User = user;
        ViewBag.Comments = comments;

        return View(post); // El modelo principal sigue siendo el post
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error al mostrar detalles del post {id}");
        return View("Error");
      }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View("Error!");
    }
  }
}