using System;
using System.Linq;
using System.Transactions;
using LearnEFWebApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnEFWebApp.Controllers
{
    public class TransactionController : Controller
    {
        private readonly DbContextOptions<LibraryContext> options;
        
        public TransactionController(DbContextOptions<LibraryContext> options)
        {
            this.options = options;
        }

        private LibraryContext CreateContext()
        {
            return new LibraryContext(options);
        }
        
        public string TestNestedTransactionDeadlock(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            UpdateInParentTransactionDeadlock(bookId);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
            
            // changes in both transaction scopes are persisted after this parent scope end
            return book.Title + " ----------- " + book.Description;
        }
        
        private void UpdateInParentTransactionDeadlock(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions {IsolationLevel = IsolationLevel.ReadUncommitted});
            
            var book = libraryContext.Books.Find(bookId);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            // outer transaction scope use Serializable isolation level by default, deadlock happen when saving change in the same table
            libraryContext.SaveChanges();
            transactionScope.Complete();
        }
        
        public string TestNestedTransactionDeadlock2(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions {IsolationLevel = IsolationLevel.ReadUncommitted});
            var book = libraryContext.Books.Find(bookId);

            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            UpdateInParentTransactionDeadlock2(bookId);
            transactionScope.Complete();
            
            // changes in both transaction scopes are persisted after this parent scope end
            return book.Title + " ----------- " + book.Description;
        }
        
        private void UpdateInParentTransactionDeadlock2(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew);
            
            // this transaction scope use Serializable isolation level, it will wait till the modification in outer transaction scope
            // to complete and lead to a deadlock
            var book = libraryContext.Books.Find(bookId);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
        }
        
        public string TestNestedTransaction(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions {IsolationLevel = IsolationLevel.ReadUncommitted});
            var book = libraryContext.Books.Find(bookId);

            UpdateInParentTransaction(bookId);
            // changes from inner transaction is persisted but not visible here
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
            
            return book.Title + " ----------- " + book.Description;
        }
        
        private void UpdateInParentTransaction(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew);
            
            // this transaction scope uses Serializable isolation level, but deadlock not occur because the table is
            // not modified in the outer transaction scope
            var book = libraryContext.Books.Find(bookId);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
        }
        
        public string TestNestedTransactionRollbackOuter(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions {IsolationLevel = IsolationLevel.ReadUncommitted});
            var book = libraryContext.Books.Find(bookId);

            UpdateInParentTransactionRollbackOuter(bookId);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            
            return book.Title + " ----------- " + book.Description;
            // rollback this transaction scope didn't affect the inner transaction scope
        }
        
        private void UpdateInParentTransactionRollbackOuter(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions {IsolationLevel = IsolationLevel.ReadUncommitted});
            
            var book = libraryContext.Books.Find(bookId);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            // changes in this transaction scope are persisted independently
            transactionScope.Complete();
        }
        
        public string TestNestedTransactionRollbackInner(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope();

            UpdateInParentTransactionRollBack(bookId);
            var book = libraryContext.Books.Find(bookId);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
            
            // only changes in this transaction scopes are persisted
            return book.Title + " ----------- " + book.Description;
        }
        
        private void UpdateInParentTransactionRollBack(int bookId)
        {
            var libraryContext = CreateContext();
            using var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew);

            var book = libraryContext.Books.Find(bookId);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
        }
        
        public string TestNestedTransactionRollbackInner2(int bookId)
        {
            var libraryContext = CreateContext();
            using var transaction = libraryContext.Database.BeginTransaction();
            var book = libraryContext.Books.Find(bookId);

            UpdateInParentTransactionRollBack2(bookId);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transaction.Commit();
            
            // changes in both transaction scopes are persisted
            return book.Title + " ----------- " + book.Description;
        }
        
        private void UpdateInParentTransactionRollBack2(int bookId)
        {
            var libraryContext = CreateContext();
            using var transaction = libraryContext.Database.BeginTransaction();
            
            var book = libraryContext.Books.Find(bookId);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transaction.Rollback();
        }
        
        public string TestMultipleContextsSameTransaction(int bookId)
        {
            var libraryContext = CreateContext();
            using var transaction = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            UpdateInSameTransaction(bookId);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transaction.Complete();
            // change made in both contexts are persisted
            
            return book.Title + " ----------- " + book.Description;
        }

        private void UpdateInSameTransaction(int bookId)
        {
            var libraryContext = CreateContext();
            var book = libraryContext.Books.Find(bookId);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
        }
        
        public string TestMultipleContextsSameTransactionRollback(int bookId)
        {
            var libraryContext = CreateContext();
            using var transaction = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            UpdateInSameTransaction(bookId);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            // don't call transaction.Complete() to rollback change
            // --> changes made in both contexts are rolled back
            
            return book.Title + " ----------- " + book.Description;
        }
        
        public string TestDifferentContextsSameTransaction(int bookId)
        {
            var libraryContext = CreateContext();
            using var transaction = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            UpdateInDifferentContextSameTransaction(bookId);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transaction.Complete();
            
            return book.Title + " ----------- " + book.Description;
        }

        private void UpdateInDifferentContextSameTransaction(in int bookId)
        {
            var builder = new DbContextOptionsBuilder<LibraryContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LibraryDemo2;");
            var libraryContext = new LibraryContext(builder.Options);
            var book = libraryContext.Books.Find(bookId);
            // PlatformNotSupportedException: This platform does not support distributed transactions.
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
        }
        
        public string TestDifferentContextsDifferentTransaction(int bookId)
        {
            var libraryContext = CreateContext();
            using var transaction = libraryContext.Database.BeginTransaction();
            var book = libraryContext.Books.Find(bookId);

            UpdateInDifferentContextDifferentTransaction(bookId);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transaction.Commit();
            
            return book.Title + " ----------- " + book.Description;
        }

        private void UpdateInDifferentContextDifferentTransaction(in int bookId)
        {
            var builder = new DbContextOptionsBuilder<LibraryContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LibraryDemo2;");
            var libraryContext = new LibraryContext(builder.Options);
            using var transaction = libraryContext.Database.BeginTransaction();
            var book = libraryContext.Books.Find(bookId);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transaction.Commit();
        }
    }
}