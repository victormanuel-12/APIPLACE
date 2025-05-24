using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PC3.Models;
using PC3.Services;
using Microsoft.AspNetCore.Identity;
using PC3.Data;
using Microsoft.EntityFrameworkCore;
namespace PC3.Controllers
{


  public class PostsController : Controller
  {
    private readonly IJsonPlaceholderService _jsonService;
    private readonly ILogger<PostsController> _logger;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _context;

    public PostsController(
        IJsonPlaceholderService jsonService,
        ILogger<PostsController> logger,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext context)
    {
      _jsonService = jsonService;
      _logger = logger;
      _userManager = userManager;
      _context = context;
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
        var currentUser = await _userManager.GetUserAsync(User);
        var currentUserId = currentUser?.Id;
        var currentUserEmail = currentUser?.Email;

        // Obtener feedback existente para este post y usuario
        var feedback = currentUser != null
            ? await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.PostId == id && f.userId == currentUserId)
            : null;
        // Pasar datos a la vista
        ViewBag.User = user;
        ViewBag.Comments = comments;
        ViewBag.Feedback = feedback;
        return View(post); // El modelo principal sigue siendo el post
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error al mostrar detalles del post {id}");
        return View("Error");
      }
    }
    [HttpPost]

    public async Task<IActionResult> AddFeedback(int postId, string sentimiento)
    {
      try
      {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
          TempData["FeedbackError"] = "Debes iniciar sesión para dejar feedback sino haz iniciado sesion registrate";
          return RedirectToAction("Details", new { id = postId });

        }

        if (sentimiento != "like" && sentimiento != "dislike")
        {
          _logger.LogWarning($"Sentimiento no válido: {sentimiento}");
          return BadRequest("Tipo de feedback no válido");
        }

        // Verificar si ya votó
        var existingFeedback = await _context.Feedbacks
            .FirstOrDefaultAsync(f => f.PostId == postId && f.userId == currentUser.Id);

        if (existingFeedback != null)
        {
          TempData["FeedbackError"] = "Ya has votado tu Feedback en este post";
        }
        else
        {
          var feedback = new Feedback
          {
            userId = currentUser.Id,
            email = currentUser.Email,
            PostId = postId,
            Sentimiento = sentimiento,
            Fecha = DateTime.UtcNow
          };
          _context.Feedbacks.Add(feedback);
          _logger.LogInformation($"Nuevo feedback creado para post {postId}");
        }

        await _context.SaveChangesAsync();
        TempData["FeedbackSuccess"] = "¡Gracias por tu feedback!";
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error inesperado al guardar feedback");
        TempData["FeedbackError"] = "Ocurrió un error inesperado";
      }

      return RedirectToAction("Details", new { id = postId });
    }

    public IActionResult ListarFeedback()
    {
      var feedbacks = _context.Feedbacks.ToList();
      return View(feedbacks);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View("Error!");
    }
  }
}