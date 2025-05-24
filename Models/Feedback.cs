using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PC3.Models
{
  public class Feedback
  {
    [Key] // Atributo que indica que es la clave primaria
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Se autoincrementa
    public int Id { get; set; }  // Identificador único del feedback

    [Required]
    [MaxLength(450)] // Longitud adecuada para IDs de Identity
    public string userId { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string email { get; set; }

    [Required]
    public int PostId { get; set; }

    [Required]
    [StringLength(10)]
    public string Sentimiento { get; set; } // "like" o "dislike"

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
  }
}