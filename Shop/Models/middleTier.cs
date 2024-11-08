using System;
using System.Collections.Generic;

namespace Shop.Models
{
    public class middleTier
    {
        public int id { get; set; }
        public string name { get; set; }
        public string city { get; set; }
        public System.DateTime startTime { get; set; }
        public System.DateTime time { get; set; }
        public string dress { get; set; }
        public string photo { get; set; }
        public int price { get; set; }
        public string type { get; set; }
        public int active;
        public int total { get; set; }
        public int aleryBuy { get; set; }
        public int count { get; set; } = 0;
        public string username { get; set; }
        public string mode { get; set; }
        public int page { get; set; } = 1;
        public string belongs { get; set; }
        public string query { get; set; }
        public string pwd { get; set; }
        public bool status { get; set; }
        public string orderNumber { get; set; }
        //详情页使用
        public string attribute { get; set; }
        public string brand { get; set; }
        public int defaultPrice { get; set; }
        public int defaultTotal { get; set; }
        public string message { get; set; }
        public string banner { get; set; }
        public string show { get; set; }
        public List<String> types { get; set; }
        public List<int> prices { get; set; }
        public List<int> totals { get; set; }
        //商家订单商品
        public List<int> babys { get; set; }
    }
}