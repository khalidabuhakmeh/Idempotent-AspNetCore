using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ContactForm.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContactForm.Pages
{
    public class StopDuplicatesMiddleware : IMiddleware
    {
        private readonly string key;
        private readonly string alertTempDataKey;

        public StopDuplicatesMiddleware(string key = "IdempotentToken", string alertTempDataKey = "AlertCookie")
        {
            this.key = key;
            this.alertTempDataKey = alertTempDataKey;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (
                context.Request.Method == HttpMethod.Post.Method &&
                context.Request.Form.TryGetValue(key, out var values))
            {
                var token = values.FirstOrDefault();
                var database = context.RequestServices.GetRequiredService<Database>();
                var factory = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
                var tempData = factory.GetTempData(context);

                try
                {
                    database.Requests.Add(new Requests
                    {
                        IdempotentToken = token
                    });
                    // we're good
                    await database.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                    when (ex.InnerException is SqliteException {SqliteErrorCode: 19})
                {
                    tempData[alertTempDataKey] = new Alert(
                            "You somehow sent this message multiple time. " +
                            "Don't worry its safe, you can carry on.",
                            "warning")
                        .ToJson();
                    tempData.Keep(alertTempDataKey);
                    
                    // a redirect and
                    // not an immediate view
                    context.Response.Redirect("/", false);
                }
            }
            
            await next(context);
        }
    }
}