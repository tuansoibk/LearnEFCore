using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnEFWebApp.Models
{
    [Table("Players", Schema = "moap")]
    public class Player
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public virtual Team Team { get; set; }
    }
}