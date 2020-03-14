using System.Collections.Generic;

namespace LearnEFWebApp.Models
{
    public class Author
    {
        public Author()
        {
            Books = new HashSet<Book>();
        }
        
        public int Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public ICollection<Book> Books { get; set; }
    }
}