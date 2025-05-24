using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PC3.Models;
namespace PC3.Services
{
  public interface IJsonPlaceholderService
  {
    Task<List<Post>?> GetPostsAsync();


    Task<Post?> GetPostAsync(int id);


    Task<List<Comment>?> GetCommentsForPostAsync(int postId);


    Task<User?> GetUserAsync(int userId);


    Task<bool> ValidarPostExiste(int postId);
  }
}