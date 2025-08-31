using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace CHERRY.Models
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Nickname { get; set; }
        public string ProfileImagePath { get; set; }
        public int PeriodLength { get; set; }
        public int CycleLength { get; set; }
    }
}