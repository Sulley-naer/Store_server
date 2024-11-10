using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    public class Orders
    {
        [Key, Column("ID")]
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

        //未开启' or '待处理' or '成功' or '失败'
        [Column("refund")]
        [DefaultValue("未开启")]
        public string refund { get; set; }

        [Column("handling_number")]
        [DefaultValue(0)]
        public int handling_number { get; set; }
    }
}
