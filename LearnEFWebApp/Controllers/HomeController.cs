using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using LearnEFWebApp.Data;
using Microsoft.AspNetCore.Mvc;
using LearnEFWebApp.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LearnEFWebApp.Controllers
{
    public class HomeController : Controller
    {
        private const string DefaultSchemaName = "dbo";
        
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private readonly LibraryContext libraryContext;

        public HomeController(LibraryContext libraryContext)
        {
            this.libraryContext = libraryContext;
        }

        public string Index()
        {
            libraryContext.Database.EnsureCreated();
            return "Hello World!";
        }

        public string Init()
        {
            try
            {
                libraryContext.Database.EnsureDeleted();
                libraryContext.Database.EnsureCreated();
                dynamic initData =
                    JsonConvert.DeserializeObject(
                        ReadFile("initdata.json"));
                SetIndentityInsert<Author>(true);
                foreach (JObject jObject in initData.authors)
                {
                    var author = jObject.ToObject<Author>();
                    libraryContext.Add(author);
                }
                libraryContext.SaveChanges();
                SetIndentityInsert<Author>(false);

                SetIndentityInsert<Book>(true);
                foreach (JObject jObject in initData.books)
                {
                    var book = jObject.ToObject<Book>();
                    libraryContext.Add(book);
                }
                libraryContext.SaveChanges();
                SetIndentityInsert<Book>(false);

                return "Done";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private void SetIndentityInsert<T>(bool on)
        {
            if (!libraryContext.Database.IsSqlServer())
            {
                return;
            }
            
            libraryContext.Database.OpenConnection();
            string command = string.Format("SET IDENTITY_INSERT {0} {1}", GetTableFullName<T>(), @on ? "ON" : "OFF");
            libraryContext.Database.ExecuteSqlRaw(command);
            libraryContext.SaveChanges();
        }

        private string GetTableFullName<T>()
        {
            var entityType = libraryContext.Model.FindEntityType(typeof(T));
            var tableName = entityType.GetTableName();

            return DefaultSchemaName + "." + tableName;
        }

        private static string ReadFile(string fileName)
        {
            FileStream fileStream = new FileStream(fileName, FileMode.Open);
            using StreamReader reader = new StreamReader(fileStream);
            return reader.ReadToEnd();
        }

        public string CreateAuthor(Author author)
        {
            if (author.Name == null)
            {
                return "Author details are missing";
            }

            try
            {
                var savedAuthor = libraryContext.Add(author);
                libraryContext.SaveChanges();
                return $"Author added, id = {savedAuthor.Entity.Id}";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string CreateBook(Book book)
        {
            if (book.Title == null || book.Description == null || book.AuthorId == 0)
            {
                return "Book details are missing";
            }

            try
            {
                var savedBook = libraryContext.Add(book);
                libraryContext.SaveChanges();
                return $"Book added, id = {savedBook.Entity.Id}";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string ReadBooks()
        {
            var books = libraryContext.Books
                .Include(b => b.Author)
                .AsNoTracking();
            return SerializeObject(books);
        }

        public string ReadBook(int id)
        {
            if (id == 0)
            {
                return "Book id is missing";
            }

            var book = libraryContext.Books.Include(b => b.Author).AsNoTracking().FirstOrDefault(b => b.Id == id);
            return SerializeObject(book);
        }

        public string ReadAuthors()
        {
            var authors = libraryContext.Authors.Include(a => a.Books).AsNoTracking();
            return SerializeObject(authors);
        }

        public string ReadAuthor(int? id)
        {
            if (id == null)
            {
                return "Author id is missing";
            }

            var author = libraryContext.Authors.Include(a => a.Books).AsNoTracking().FirstOrDefault(a => a.Id == id);
            return SerializeObject(author);
        }

        public string UpdateBook(int bookId, Book newBook)
        {
            if (bookId == 0)
            {
                return "Book id is missing";
            }

            if (bookId != newBook.Id)
            {
                return "Invalid data";
            }

            var dbBook = libraryContext.Books.Find(bookId);
            if (dbBook == null)
            {
                return $"Book with id = {bookId} not found";
            }

            bool hasUpdate = false;
            if (dbBook.Title != newBook.Title)
            {
                dbBook.Title = newBook.Title;
                hasUpdate = true;
            }

            if (dbBook.Description != newBook.Description)
            {
                dbBook.Description = newBook.Description;
                hasUpdate = true;
            }

            if (dbBook.AuthorId != newBook.AuthorId)
            {
                if (libraryContext.Authors.Find(newBook.AuthorId) == null)
                {
                    return $"Author with id = {newBook.AuthorId} not found";
                }

                dbBook.AuthorId = newBook.AuthorId;
                hasUpdate = true;
            }

            if (hasUpdate)
            {
                libraryContext.SaveChanges();
                return "Updated successfully";
            }
            else
            {
                return "New book details are the same, no update";
            }
        }

        public string DeleteBook(int id)
        {
            if (id == 0)
            {
                return "Book id is missing";
            }

            var dbBook = libraryContext.Books.Find(id);
            if (dbBook == null)
            {
                return $"Book with id = {id} not found";
            }

            try
            {
                libraryContext.Books.Remove(dbBook);
                libraryContext.SaveChanges();
                return $"Book with id = {id} deleted";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string SortBooks(string sortBy)
        {
            sortBy = sortBy?.ToLower();

            var books = libraryContext.Books.Include(b => b.Author).AsNoTracking();
            if (sortBy == nameof(Book.Title).ToLower())
            {
                books = books.OrderBy(b => b.Title);
            }
            else if (sortBy == nameof(Book.Description).ToLower())
            {
                books = books.OrderBy(b => b.Description);
            }
            else if (sortBy == nameof(Book.Author).ToLower())
            {
                books = books.OrderBy(b => b.Author.Name);
            }
            // else: sort by id by default

            return SerializeObject(books);
        }
        
        public string FindBooks(string fieldName, string value)
        {
            fieldName = fieldName?.ToLower();

            var books = libraryContext.Books.Include(b => b.Author).AsNoTracking();
            if (fieldName == nameof(Book.Title).ToLower())
            {
                books = books.Where(b => b.Title.Contains(value));
            }
            else if (fieldName == nameof(Book.Description).ToLower())
            {
                books = books.Where(b => b.Description.Contains(value));
            }
            else if (fieldName == nameof(Book.Author).ToLower())
            {
                books = books.Where(b => b.Author.Name.Contains(value));
            }
            else
            {
                // find by id by default
                books = books.Where(b => b.Id == int.Parse(value));
            }

            return SerializeObject(books);
        }

        public string ReadBookNative(int id)
        {
            // 1st option: use interpolated method with interpolated string
            var books = libraryContext.Books.FromSqlInterpolated($"select * from Books book where id={id}").ToList();
            
            // 2nd option: use raw method with format string
            //var books = libraryContext.Books.FromSqlRaw("select * from Books where id={0}",id);
            
            // 3rd option: use raw method with sql parameter string => drawback: must know DB type
            // var books = libraryContext.Books.FromSqlRaw("select * from Books where id=@id",
            //     new SqliteParameter("@id", id));
            foreach (var book in books)
            {
                libraryContext.Entry(book).Reference(b => b.Author).Load();
                // libraryContext.Entry(book.Author).Reload(); --> yeild ArgumentNullException as book.Author = null
            }

            return SerializeObject(books);
        }

        public string DeleteBookNative(int id)
        {
            int rowCount = libraryContext.Database.ExecuteSqlInterpolated($"delete from Books where id={id}");

            return $"{rowCount} book(s) deleted";
        }
        
        public string ReadBookTitles()
        {
            // demo ADO
            using DbConnection connection = libraryContext.Database.GetDbConnection();
            connection.Open();
            DbCommand command = connection.CreateCommand();
            command.CommandText = "select Title from Books";
            DbDataReader reader = command.ExecuteReader();
            StringBuilder builder = new StringBuilder();
            while (reader.Read())
            {
                builder.AppendLine(reader.GetString(0));
            }

            return builder.ToString();
        }

        public string TestLazyLoading(int bookId)
        {
            //var book = libraryContext.Books.Find(bookId);
            
            // eager loading
            var book = libraryContext.Books.Include(b => b.Author).First(b => b.Id == bookId);
            
            // explicit load
            //libraryContext.Entry(book).Reference(b => b.Author).Load();
            
            // lazy load by reference
            var author = book.Author;
            
            // this statement will load reference entities of given book & author
            //return SerializeObject(book) + "\r\n----------------------\r\n" + SerializeObject(author);
            
            // this statement will not load reference entity
            return book + "\r\n----------------------\r\n" + author;
        }

        public string TestSave(int bookId)
        {
            var book = libraryContext.Books.Find(bookId);
            book.Title += DateTime.Now.ToString().Last();
            book.Author.Address += DateTime.Now.ToString().Last();
            // SaveChanges must be called for tracked entity
            libraryContext.SaveChanges();

            return book.Title  + "\r\n----------------------\r\n" + book.Author.Address;
        }
        
        public string TestSaveOfNoTracking(int bookId)
        {
            var book = libraryContext.Books.AsNoTracking().First(b => b.Id == bookId);
            book.Title += DateTime.Now.ToString().Last();

            // lazy loading is not supported for detached entity
            //var author = book.Author;

            // setting entity state == attach the entity again
            libraryContext.Entry(book).State = EntityState.Modified;
            //var author = book.Author;
            
            // setting entity entry current value doesn't attach the entity again
            // --> can't use lazy loading afterward
            // --> can't save the value as well
            // have to load the entity again and SetValues
            // var book1 = libraryContext.Books.Find(bookId);
            // libraryContext.Entry(book1).CurrentValues.SetValues(book);

            libraryContext.SaveChanges();

            return book.Title + "\r\n----------------------\r\n";
        }
        
        public string TestSaveFakeBook(int bookId)
        {
            var book = new Book
            {
                Id = bookId,
                Title = DateTime.Now.ToString(),
                AuthorId = 2
            };

            // new entity object can still be attached & saved by setting entity state
            libraryContext.Entry(book).State = EntityState.Modified;
            libraryContext.SaveChanges();

            return book.Title;
        }

        public string TestSaveWithTransaction(int bookId)
        {
            using (var transaction = libraryContext.Database.BeginTransaction())
            {
                var book = libraryContext.Books.Find(bookId);
                book.Title += DateTime.Now.ToString().Last();

                // SaveChanges must be called even in a transaction
                Console.WriteLine("SaveChanges() is about to be invoked");
                libraryContext.SaveChanges();
                Console.WriteLine("SaveChanges() invoked");
                book.Author.Address += DateTime.Now.ToString().Last();
                Console.WriteLine("Commit() is about to be invoked");
                // changes will only be persisted once Commit() is invoked
                transaction.Commit();
                Console.WriteLine("Commit() invoked");
            }
            
            var book2 = libraryContext.Books.Find(bookId);

            return book2.Title  + "\r\n----------------------\r\n" + book2.Author.Address;
        }
        
        public string TestSaveWithTransactionRollBack(int bookId)
        {
            using (var transaction = libraryContext.Database.BeginTransaction())
            {
                try
                {
                    var book = libraryContext.Books.Find(bookId);
                    book.Title += DateTime.Now.ToString().Last();

                    // SaveChanges must be called even in a transaction
                    Console.WriteLine("SaveChanges() is about to be invoked");
                    libraryContext.SaveChanges();
                    Console.WriteLine("SaveChanges() invoked");
                    throw new Exception("test");
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                }
            }
            
            var book2 = libraryContext.Books.Find(bookId);

            return book2.Title  + "\r\n----------------------\r\n" + book2.Author.Address;
        }

        private string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, JsonSerializerSettings);
        }
    }
}