using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LearnEFWebApp.Controllers
{
    public class TransactionalAttribute : Attribute, IFilterFactory
    {
        private Type[] contextTypes;
        
        public TransactionalAttribute(params Type[] types)
        {
            contextTypes = types;
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new TransactionalFilter(contextTypes);
        }

        public bool IsReusable => false;

        private class TransactionalFilter : IActionFilter
        {
            private List<Type> contextTypes;
            private List<IDbContextTransaction> transactions;
        
            public TransactionalFilter(params Type[] types)
            {
                contextTypes = new List<Type>(types);
                transactions = new List<IDbContextTransaction>();
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                foreach (Type contextType in contextTypes)
                {
                    if (typeof(DbContext).IsAssignableFrom(contextType))
                    {
                        var dbContext = (DbContext) context.HttpContext.RequestServices.GetRequiredService(contextType);
                        if (dbContext != null)
                        {
                            transactions.Add(dbContext.Database.BeginTransaction());
                        }
                    }
                }
            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
                foreach (Type contextType in contextTypes)
                {
                    if (typeof(DbContext).IsAssignableFrom(contextType))
                    {
                        var dbContext = (DbContext) context.HttpContext.RequestServices.GetRequiredService(contextType);
                        dbContext?.SaveChanges();
                    }
                }
                foreach (var dbContextTransaction in transactions)
                {
                    dbContextTransaction.Commit();
                    dbContextTransaction.Dispose();
                }
            }
        }
    }
}