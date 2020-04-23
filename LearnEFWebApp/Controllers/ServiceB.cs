using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using LearnEFWebApp.Data;
using LearnEFWebApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace LearnEFWebApp.Controllers
{
    public class ServiceB
    {
        private readonly LibraryContext libraryContext;
        
        public ServiceB(IServiceProvider serviceProvider)
        {
            libraryContext = serviceProvider.GetRequiredService<LibraryContext>();
        }

        public void UpdateBook(Book book)
        {
            using var scope = new TransactionScope(TransactionScopeOption.RequiresNew);

            
            book.Title += DateTime.Now.Ticks.ToString().Last();
            libraryContext.SaveChanges();
            scope.Complete();
        }
    }
}