using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    [Table(("shoppingCart"))]
    public partial class User_shoppingCart
    {
        [Key]
        public int id { get; set; }

        public string account { get; set; }

        public int item { get; set; }

        public int count { get; set; } = 0;

        [Column("type")]
        [DefaultValue("default")]
        public string type { get; set; }

        [ForeignKey(nameof(item))]
        public virtual babys babys { get; set; }
    }
}
