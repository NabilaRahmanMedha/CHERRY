using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using CHERRY.Models;
using System.Security.Cryptography;

namespace CHERRY.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _db;

        public DatabaseService(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<User>().Wait();
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public async Task<bool> RegisterUserAsync(string email, string password)
        {
            var existingUser = await _db.Table<User>().FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null) return false; 

            var newUser = new User
            {
                Email = email,
                PasswordHash = HashPassword(password)
            };

            await _db.InsertAsync(newUser);
            return true;
        }

        public async Task<User?> LoginUserAsync(string email, string password)
        {
            var hashedPassword = HashPassword(password);
            return await _db.Table<User>()
                            .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == hashedPassword);
        }
    }
}
