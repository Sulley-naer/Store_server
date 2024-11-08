using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    [Table("DetailsPhoto")]
    public class DetailsPhoto
    {
        [Key]
        public int id { get; set; }
        public string Photo  { get; set; }
        public int Baby { get; set; }
        public string Types { get; set; }
        public string position { get; set; }
        public int price { get; set; }
        public int total { get; set; }
        public int alreadyBuy {get;set;}
        public string belong { get; set; }
    }
}