using System.ComponentModel.DataAnnotations;

namespace LearnEFWebApp.Models
{
    public class Book
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        public string Isbn { get; set; }
        
        public int AuthorId { get; set; }

        public virtual Author Author { get; set; }
    }
}