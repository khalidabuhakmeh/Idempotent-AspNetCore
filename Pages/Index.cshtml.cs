using System;
using System.Text.Json;
using System.Threading.Tasks;
using ContactForm.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContactForm.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> logger;
        private readonly Database database;

        [BindProperty] public string IdempotentToken { get; set; }
        [BindProperty] public string Text { get; set; }

        [TempData] public string AlertCookie { get; set; }

        public Alert Alert =>
            AlertCookie is not null
                ? JsonSerializer.Deserialize<Alert>(AlertCookie)
                : null;

        public IndexModel(ILogger<IndexModel> logger, Database database)
        {
            this.logger = logger;
            this.database = database;
        }

        public void OnGet()
        {
            IdempotentToken = Guid.NewGuid().ToString();
            //IdempotentToken = "b613c592-13a1-492d-a6e0-151b0e9996fd";
        }
        
        public async Task<IActionResult> OnPost()
        {
            try
            {
                if (string.IsNullOrEmpty(IdempotentToken))
                {
                    AlertCookie = Alert.Error.ToJson();
                    return Page();
                }

                database.Messages.Add(new Message
                {
                    IdempotentToken = IdempotentToken,
                    Text = Text
                });

                // will throw if unique
                // constraint is violated
                await database.SaveChangesAsync();

                TempData[nameof(AlertCookie)] =
                    new Alert("Successfully received message").ToJson();

                // perform Redirect -> Get
                return RedirectToPage();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqliteException {SqliteErrorCode: 19})
            {
                AlertCookie = new Alert(
                    "You somehow sent this message multiple time. " +
                        "Don't worry its safe, you can carry on.", 
                    "warning")
                .ToJson();
            }
            catch
            {
                AlertCookie = Alert.Error.ToJson();
            }

            return Page();
        }
    }

    public record Alert(string Text, string CssClass = "success")
    {
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static Alert Error { get; } = new(
            "We're not sure what happened.",
            "warning"
        );
    };
}