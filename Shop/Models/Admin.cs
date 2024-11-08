using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    [Table("Admin")]
    public class Admin
    {
        [Key]
        public int Id { get; set; }
        [Column("account")]
        public string account { get; set; }
        [Column("pwd")]
        public string pwd { get; set; }
        [Column("authority")]
        public int authority { get; set; }
    }
}