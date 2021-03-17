using System;
using Microsoft.EntityFrameworkCore;

namespace ContactForm.Models
{
    public class Database : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite("Data source=communication.db");

        public DbSet<Message> Messages { get; set; }
        public DbSet<Requests> Requests { get; set; }
    }

    [Index(nameof(IdempotentToken), IsUnique = true)]
    public class Message
    {
        public int Id { get; set; }
        public string Text { get; set; }
        
        public string IdempotentToken { get; set; }
    }

    [Index(nameof(IdempotentToken), IsUnique = true)]
    public class Requests
    {
        public int Id { get; set; }
        public string IdempotentToken { get; set; }

        public static string New()
        {
            return Guid.NewGuid().ToString();
        }
    }
}