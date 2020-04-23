using System;
using System.Linq;
using System.Transactions;
using LearnEFWebApp.Data;
using LearnEFWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace LearnEFWebApp.Controllers
{
    public class TestController : Controller
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        
        private readonly LibraryContext libraryContext;
        private readonly ServiceA serviceA;
        private readonly ServiceB serviceB;
        
        public TestController(LibraryContext libraryContext, ServiceA serviceA, ServiceB serviceB)
        {
            this.libraryContext = libraryContext;
            this.serviceA = serviceA;
            this.serviceB = serviceB;
        }

        public string TestNoTransaction(int bookId)
        {
            var book = libraryContext.Books.Find(bookId);

            book.Title += DateTime.Now.Ticks.ToString().Last();
            // in this method, db changes are sent to db server & persisted every time SaveChanges() is call
            libraryContext.SaveChanges();
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();

            return book.Title + " ----------- " + book.Description;
        }
        
        public string TestTransactionScope(int bookId)
        {
            using var transactionScope = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            book.Title += DateTime.Now.Ticks.ToString().Last();
            // in this method, db changes are sent to db server SaveChanges() is call, but not persisted
            libraryContext.SaveChanges();
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();

            transactionScope.Complete();

            // changes are only persisted into db after the transaction scope is ended, in this case, after the return call
            return book.Title + " ----------- " + book.Description;
        }
        
        public string TestTransactionScopeRollBack(int bookId)
        {
            using var transactionScope = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            book.Title += DateTime.Now.Ticks.ToString().Last();
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();

            // in this method, changes are sent to db, but not persisted because transactionScope.Completed() is not call
            // --> this is the method to rollback change when using transaction scope
            return book.Title + " ----------- " + book.Description;
        }

        public string TestNestedTransactionDefault(int bookId)
        {
            using var transactionScope = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            UpdateInParentTransaction(book);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
            
            // changes in both methods are persisted into db after the transaction scope in this method is ended
            // in this case, after the return call
            return book.Title + " ----------- " + book.Description;
        }

        private void UpdateInParentTransaction(Book book)
        {
            // by default, no new transaction scope is created, it uses the ambient transaction scope
            using var transactionScope = new TransactionScope();
            
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
            // changes are not persisted to db after the transaction scope in this method is ended
        }
        
        public string TestNestedTransactionRequireNew(int bookId)
        {
            using var transactionScope = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            UpdateInParentTransactionRequireNew(book);
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
            
            return book.Title + " ----------- " + book.Description;
        }

        private void UpdateInParentTransactionRequireNew(Book book)
        {
            // new transaction scope is created
            using var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew);
            
            book.Description += DateTime.Now.Ticks.ToString().Last();
            // nested transaction is not supported for the same context
            // below call will throw an exception of "InvalidOperationException:
            // This connection was used with an ambient transaction. The original ambient transaction needs to be
            // completed before this connection can be used outside of it."
            libraryContext.SaveChanges();
            transactionScope.Complete();
        }
        
        public string TestNestedTransactionInService(int bookId)
        {
            using var transactionScope = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            // inside service A, the db context is still the same with current db context
            // same error with nested transaction for single context
            serviceA.UpdateBook(book);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
            
            return book.Title + " ----------- " + book.Description;
        }
        
        public string TestNestedTransactionInService2(int bookId)
        {
            using var transactionScope = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            // inside service A, the db context is still the same with current db context, even it is resolved from ServiceProvider
            // same error with nested transaction for single context
            // the db context is scoped per request, not per resolution
            serviceB.UpdateBook(book);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
            
            return book.Title + " ----------- " + book.Description;
        }
        
        public string TestNestedTransactionInService3(int bookId)
        {
            using var transactionScope = new TransactionScope();
            var book = libraryContext.Books.Find(bookId);

            // inside service A, the db context is still the same with current db context
            // same error with nested transaction for single context
            serviceA.UpdateBookWithCommittableTransaction(book);
            book.Description += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transactionScope.Complete();
            
            return book.Title + " ----------- " + book.Description;
        }
        
        public string TestNestedBeginTransaction(int bookId)
        {
            using var transaction = libraryContext.Database.BeginTransaction();
            var book = libraryContext.Books.Find(bookId);

            // InvalidOperationException: The connection is already in a transaction and cannot participate in another transaction.
            using (var transaction2 = libraryContext.Database.BeginTransaction())
            {
                book.Description += DateTime.Now.Ticks.ToString().Last();
                libraryContext.SaveChanges();
                transaction2.Commit();
            }
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            transaction.Commit();
            
            return book.Title + " ----------- " + book.Description;
        }
    }
}