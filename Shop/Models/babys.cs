using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    [Table("babys")]
    public partial class babys
    {
        [Column("id")]
        public int id { get; set; }

        public string name { get; set; }
        public string city { get; set; }
        public System.DateTime time { get; set; }
        public string address { get; set; }
        public string photo { get; set; }
        public int price { get; set; }
        public string type { get; set; }
        public int total { get; set; }
        public int alreadyBuy { get; set; }
        public string belongs { get; set; }
        public string brand { get; set; }
        public string message { get; set; }
        public string attribute { get; set; } = "really";

        [NotMapped]
        public int active;

        [NotMapped]
        [DefaultValue("未选择")]
        public string selectedStyle { get; set; }
    }
}
