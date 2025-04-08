using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using RuychWeb.Repository.Abstract;

namespace RuychWeb.Middleware
{
    public class DatabaseInitializerMiddleware
    {
        private readonly RequestDelegate _next;

        public DatabaseInitializerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IDatabaseInitializer databaseInitializer)
        {
            await databaseInitializer.SeedAdminAccountAsync();
            await _next(context);
        }
    }
}