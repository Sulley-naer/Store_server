using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    public class Orders
    {
        [Key,Column("ID")]
        public int ID { get; set; }
        [Column("orderNumber")]
        public Guid orderNumber { get; set; }
        [Column("belong")]
        public string belong { get; set; }
        [Column("baby")]
        public string baby { get; set; }
        [Column("time")]
        public DateTime time { get; set; }
        [Column("status")]
        public bool status { get; set; }
        [Column("logistics")]
        [DefaultValue("未发货")]
        public string logistics { get; set; }
    }
}