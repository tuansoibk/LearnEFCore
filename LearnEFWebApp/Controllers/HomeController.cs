using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LearnEFWebApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LearnEFWebApp.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LearnEFWebApp.Controllers
{
    public class HomeController : Controller
    {
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
                foreach (JObject jObject in initData.authors)
                {
                    var author = jObject.ToObject<Author>();
                    libraryContext.Add(author);
                }

                foreach (JObject jObject in initData.books)
                {
                    var book = jObject.ToObject<Book>();
                    libraryContext.Add(book);
                }

                libraryContext.SaveChanges();
                return "Done";
            }
            catch (Exception e)
            {
                return e.Message;
            }
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
            var books = libraryContext.Books.Include(b => b.Author).AsNoTracking();
            return JsonConvert.SerializeObject(books, Formatting.Indented, JsonSerializerSettings);
        }

        public string ReadBook(int id)
        {
            if (id == 0)
            {
                return "Book id is missing";
            }

            var book = libraryContext.Books.Include(b => b.Author).AsNoTracking().FirstOrDefault(b => b.Id == id);
            return JsonConvert.SerializeObject(book, Formatting.Indented, JsonSerializerSettings);
        }

        public string ReadAuthors()
        {
            var authors = libraryContext.Authors.Include(a => a.Books).AsNoTracking();
            return JsonConvert.SerializeObject(authors, Formatting.Indented, JsonSerializerSettings);
        }

        public string ReadAuthor(int? id)
        {
            if (id == null)
            {
                return "Author id is missing";
            }

            var author = libraryContext.Authors.Include(a => a.Books).AsNoTracking().FirstOrDefault(a => a.Id == id);
            return JsonConvert.SerializeObject(author, Formatting.Indented, JsonSerializerSettings);
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

            return JsonConvert.SerializeObject(books, Formatting.Indented, JsonSerializerSettings);
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

            return JsonConvert.SerializeObject(books, Formatting.Indented, JsonSerializerSettings);
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

            return JsonConvert.SerializeObject(books, Formatting.Indented, JsonSerializerSettings);
        }

        public string DeleteBookNative(int id)
        {
            //int rowCount = libraryContext;

            return string.Empty;
        }
    }
}