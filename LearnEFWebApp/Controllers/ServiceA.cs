using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using LearnEFWebApp.Data;
using LearnEFWebApp.Models;

namespace LearnEFWebApp.Controllers
{
    public class ServiceA
    {
        private readonly LibraryContext libraryContext;
        
        public ServiceA(LibraryContext libraryContext)
        {
            this.libraryContext = libraryContext;
        }

        public void UpdateBook(Book book)
        {
            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew);

            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            scope.Complete();
        }
        
        public void UpdateBookWithCommittableTransaction(Book book)
        {
            using var scope = new TransactionScope(new CommittableTransaction());

            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            scope.Complete();
        }
    }
}