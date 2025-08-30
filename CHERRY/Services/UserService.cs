using CHERRY.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CHERRY.Services
{
    public class UserService
    {
        private readonly DatabaseService _db;

        public UserService()
        {
            _db = new DatabaseService();
        }

        public async Task<User> GetUserProfileAsync(string email)
        {
            return await _db.GetUserByEmailAsync(email);
        }

        public async Task<bool> UpdateProfileImageAsync(string email, string imagePath)
        {
            try
            {
                var user = await GetUserProfileAsync(email);
                if (user != null)
                {
                    user.ProfileImagePath = imagePath;
                    return await _db.UpdateUserAsync(user);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserProfileAsync(User user)
        {
            return await _db.UpdateUserAsync(user);
        }

        public async Task<bool> DeleteUserAsync(string email)
        {
            try
            {
                var user = await GetUserProfileAsync(email);
                if (user != null)
                {
                    // Delete profile image if exists
                    if (!string.IsNullOrEmpty(user.ProfileImagePath) && File.Exists(user.ProfileImagePath))
                    {
                        File.Delete(user.ProfileImagePath);
                    }

                    return await _db.DeleteUserAsync(email);
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