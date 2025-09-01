using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using CHERRY.Models;
using System.Security.Cryptography;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace CHERRY.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _db;

        public DatabaseService()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "cherry.db3");
            Debug.WriteLine("=== DB PATH: " + dbPath + " ===");
            _db = new SQLiteAsyncConnection(dbPath);

            
            InitializeDatabase();
            
           

        }

        private async void InitializeDatabase()
        {
            // Create User table if it doesn't exist
            await _db.CreateTableAsync<User>();

            // Check if we need to add new columns to existing table
            var tableInfo = await _db.GetTableInfoAsync("User");
            if (!tableInfo.Any(c => c.Name == "Nickname"))
            {
                // Add new columns to existing table
                await _db.ExecuteAsync("ALTER TABLE User ADD COLUMN Nickname TEXT");
                await _db.ExecuteAsync("ALTER TABLE User ADD COLUMN ProfileImagePath TEXT");
                await _db.ExecuteAsync("ALTER TABLE User ADD COLUMN PeriodLength INTEGER DEFAULT 0");
                await _db.ExecuteAsync("ALTER TABLE User ADD COLUMN CycleLength INTEGER DEFAULT 0");
            }
        }

        // Local auth removed. API-based auth is used. This service now only manages local profile cache.

        // New method to get user by email
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _db.Table<User>().FirstOrDefaultAsync(u => u.Email == email);
        }

        // New method to update user profile
        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                await _db.UpdateAsync(user);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // New method to delete user
        public async Task<bool> DeleteUserAsync(string email)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                if (user != null)
                {
                    await _db.DeleteAsync(user);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}