using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp
{
    [Table("UserData")]
    public class dbUserData
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int WpId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
        public string Email { get; set; }
        public string Language { get; set; }
        public string Channel { get; set; }
        public string Channels { get; set; }
        public DateTime UserRegistered { get; set; }
        public string Token { get; set; }
        public DateTime TokenCreation { get; set; }
        public DateTime ProgressDate { get; set; }
        public DateTime ActionDate { get; set; }
        public DateTime CreditCardUpdateDate { get; set; }
    }
}
