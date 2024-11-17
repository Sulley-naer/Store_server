using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    [Table("shipped")]
    public class shipped
    {
        [Key]
        public int id { get; set; }
        [Column("shop")]
        public string shop { get; set; }
        [Column("baby")]
        public int baby { get; set; }
        [Column("logistics")]
        [DefaultValue("暂未发货")]
        public string logistics { get; set; }
        [Column("bind-order")]
        public int bindOrder { get; set; }
    }
}