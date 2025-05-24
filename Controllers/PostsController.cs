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
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
      try
      {
        var posts = await _jsonService.GetPostsAsync();
        return View(posts);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error al obtener la lista de posts");
        return View("Error");
      }
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
      if (id <= 0)
      {
        _logger.LogWarning($"ID de post inválido: {id}");
        return BadRequest("ID de post inválido");
      }

      _logger.LogInformation($"Obteniendo post con id: {id}");

      try
      {
        var post = await _jsonService.GetPostAsync(id);
        if (post == null)
        {
          _logger.LogWarning($"No se encontró el post con id: {id}");
          return NotFound($"No se encontró el post con ID {id}");
        }

        var user = await _jsonService.GetUserAsync(post.userId);


        var comments = await _jsonService.GetCommentsForPostAsync(id) ?? new List<Comment>();
        _logger.LogInformation($"Encontrados {comments.Count} comentarios para post {id}");

        // Manejo seguro del usuario actual
        string currentUserId = null;
        string currentUserEmail = null;
        Feedback feedback = null;

        if (User.Identity.IsAuthenticated)
        {
          try
          {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
              currentUserId = currentUser.Id;
              currentUserEmail = currentUser.Email;

              // Obtener feedback existente para este post y usuario
              feedback = await _context.Feedbacks
                  .AsNoTracking()
                  .FirstOrDefaultAsync(f => f.PostId == id && f.userId == currentUserId);
            }
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Error al obtener información del usuario actual");
            // No fallar la página completa por esto
          }
        }

        // Pasar datos a la vista
        ViewBag.User = user;
        ViewBag.Comments = comments;
        ViewBag.Feedback = feedback;
        ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
        ViewBag.CurrentUserEmail = currentUserEmail;

        return View(post);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error al mostrar detalles del post {id}");
        return View("Error");
      }
    }

    [HttpPost]
    [Authorize] // Requiere autenticación
    [ValidateAntiForgeryToken] // Previene ataques CSRF
    public async Task<IActionResult> AddFeedback(int postId, string sentimiento)
    {
      if (postId <= 0)
      {
        _logger.LogWarning($"PostId inválido: {postId}");
        return BadRequest("ID de post inválido");
      }

      if (string.IsNullOrWhiteSpace(sentimiento) ||
          (sentimiento != "like" && sentimiento != "dislike"))
      {
        _logger.LogWarning($"Sentimiento no válido: {sentimiento}");
        TempData["FeedbackError"] = "Tipo de feedback no válido";
        return RedirectToAction("Details", new { id = postId });
      }

      try
      {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
          _logger.LogWarning("Usuario autenticado pero no encontrado en UserManager");
          TempData["FeedbackError"] = "Error de sesión. Por favor, inicia sesión nuevamente.";
          return RedirectToAction("Details", new { id = postId });
        }

        // Verificar si ya votó usando transacción para evitar condiciones de carrera
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
          var existingFeedback = await _context.Feedbacks
              .FirstOrDefaultAsync(f => f.PostId == postId && f.userId == currentUser.Id);

          if (existingFeedback != null)
          {
            TempData["FeedbackError"] = "Ya has votado tu Feedback en este post";
            _logger.LogInformation($"Usuario {currentUser.Email} intentó votar duplicado en post {postId}");
          }
          else
          {
            var feedback = new Feedback
            {
              userId = currentUser.Id,
              email = currentUser.Email ?? "sin-email",
              PostId = postId,
              Sentimiento = sentimiento,
              Fecha = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["FeedbackSuccess"] = "¡Gracias por tu feedback!";
            _logger.LogInformation($"Nuevo feedback creado para post {postId} por usuario {currentUser.Email}");
          }
        }
        catch
        {
          await transaction.RollbackAsync();
          throw;
        }
      }
      catch (DbUpdateException ex)
      {
        _logger.LogError(ex, $"Error de base de datos al guardar feedback para post {postId}");
        TempData["FeedbackError"] = "Error al guardar tu feedback. Inténtalo nuevamente.";
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error inesperado al guardar feedback para post {postId}");
        TempData["FeedbackError"] = "Ocurrió un error inesperado. Inténtalo nuevamente.";
      }

      return RedirectToAction("Details", new { id = postId });
    }

    [Authorize] // Solo usuarios autenticados pueden ver feedbacks
    public async Task<IActionResult> ListarFeedback()
    {
      try
      {
        var feedbacks = await _context.Feedbacks
            .AsNoTracking()
            .OrderByDescending(f => f.Fecha)
            .ToListAsync();

        return View(feedbacks);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error al obtener lista de feedbacks");
        return View("Error");
      }
    }

    // Acción para verificar el estado de autenticación (útil para AJAX)
    [HttpGet]
    public IActionResult CheckAuthStatus()
    {
      return Json(new
      {
        isAuthenticated = User.Identity.IsAuthenticated,
        userName = User.Identity.Name
      });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel
      {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
      });
    }
  }
}