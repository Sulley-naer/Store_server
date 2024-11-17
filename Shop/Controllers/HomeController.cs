using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using Shop.Models;

namespace Shop.Controllers
{
    [RoutePrefix("api")]
    public class HomeController : ApiController
    {
        private Model db = new Model();

        private static IQueryable<babys> BeforeDate = null;

        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet]
        [Route("test")]
        public IHttpActionResult Test()
        {
            return Ok(
                new
                {
                    Admin = db.Admin.OrderBy(x => x.account).Take(1).ToList(),
                    babys = db.babys.OrderBy(x => x.id).Take(1).ToList(),
                    User_shoppingCart = db.User_shoppingCart.OrderBy(x => x.item).Take(1).ToList(),
                    Orders = db.Orders.OrderBy(x => x.ID).Take(1).ToList(),
                    DetailsPhoto = db.DetailsPhoto.OrderBy(x => x.id).Take(1).ToList(),
                    PlayerList = db.PlayerList.OrderBy(x => x.id).Take(1).ToList()
                }
            );
        }

        //登录
        [HttpPost]
        [Route("login")]
        public PlayerList Login([FromBody] PlayerList value)
        {
            PlayerList res = db.PlayerList.FirstOrDefault(o => o.account.Equals(value.account));
            if (res != null)
            {
                return res;
            }
            else
            {
                return null;
            }
            ;
        }

        //注册
        [HttpPost]
        [Route("register")]
        public bool Reg([FromBody] PlayerList value)
        {
            db.PlayerList.Add(value);
            try
            {
                return db.SaveChanges() > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("重复键错误");
                return false;
            }
        }

        //获取商品
        [HttpPost]
        [Route("GetBabyList")]
        public List<babys> GetBabyList([FromBody] middleTier pages)
        {
            // 获取符合筛选条件的babys记录
            BeforeDate = db.babys.Where(x =>
                (pages.city == null || x.city == pages.city)
                && // 城市筛选
                (pages.type == null || x.type == pages.type)
                && // 类型筛选
                (pages.name == null || x.name.Contains(pages.name))
                && // 商家筛选
                (pages.belongs == null || x.belongs.Contains(pages.belongs))
                && // 名称搜索
                (pages.time.Year < 1900 || x.time >= pages.time) // 时间筛选
            );

            if (pages.mode == "search")
                return BeforeDate.OrderBy(x => x.id).Skip((pages.page - 1) * 10).Take(10).ToList();

            // 获取筛选后的babys列表，并通过Join查询shoppingCart中的记录
            var result = BeforeDate.OrderBy(x => x.id).Skip((pages.id - 1) * 10).Take(9).ToList();

            // 获取当前用户的account
            var currentUserAccount = pages.username;

            // 为每个baby查找对应的购物车记录并设置active值
            foreach (var baby in result)
            {
                // 查找shoppingCart中与当前用户和物品相关的记录
                var shoppingCartItem = db.User_shoppingCart.FirstOrDefault(sc =>
                    sc.account == currentUserAccount && sc.item == baby.id
                );

                // 如果找到记录，设置active为对应的count值，否则为0
                baby.active = shoppingCartItem?.count ?? 0;
            }

            return result;
        }

        //最大页数 TODO 后续废弃，单接口返回
        [HttpPost]
        [Route("GetTotalBabyPages")]
        public int GetTotalBabyPages()
        {
            int totalRecords = db.babys.Count(); // 获取总记录数

            if (BeforeDate == null)
            {
                return totalRecords;
            }
            else
            {
                return BeforeDate.Count();
            }
        }

        //🛒购物车
        [HttpPost]
        [Route("BabyCar")]
        public List<User_shoppingCart> BabyCar([FromBody] User_shoppingCart value)
        {
            if (value.account != null)
            {
                var res = db.User_shoppingCart.Where(x => x.account.Equals(value.account)).ToList();
                return res;
            }
            else
            {
                return null;
            }
        }

        //购物车物品数量
        [HttpPost]
        [Route("CarCount")]
        public IHttpActionResult CarCount([FromBody] User_shoppingCart value)
        {
            if (value.account != null)
                return BadRequest("异常请求");

            return Ok(db.User_shoppingCart.Where(x => x.account.Equals(value.account)).Count());
        }

        //🛒添加购物车
        [HttpPost]
        [Route("AddBabyCar")]
        public IHttpActionResult AddBabyCar([FromBody] User_shoppingCart value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.account != null && value.item > 0)
            {
                if (
                    db.User_shoppingCart.FirstOrDefault(x =>
                        x.account == value.account && x.item == value.item && x.type == value.type
                    ) == null
                )
                {
                    db.User_shoppingCart.Add(value);
                    return Ok(db.SaveChanges() > 0);
                }
                else
                {
                    // ReSharper disable once PossibleNullReferenceException
                    db
                        .User_shoppingCart.FirstOrDefault(x =>
                            x.account.Equals(value.account)
                            && x.item.Equals(value.item)
                            && x.type == value.type
                        )
                        .count = value.count;
                }
                return Ok(db.SaveChanges() > 0);
            }
            else
            {
                return BadRequest("请求异常");
            }
        }

        //🛒删除购物车
        [HttpPost]
        [Route("DelBabyCar")]
        public bool DelBabyCar([FromBody] User_shoppingCart value)
        {
            if (value.account != null && value.item > 0)
            {
                var entity = db.User_shoppingCart.FirstOrDefault(x =>
                    x.account == value.account && x.item == value.item
                );

                if (entity != null)
                {
                    db.User_shoppingCart.Remove(entity);
                    return db.SaveChanges() > 0;
                }
            }

            return false;
        }

        //🛒获取用户购物车
        [HttpPost]
        [Route("GetUserCar")]
        public IHttpActionResult GetUserCar(PlayerList account)
        {
            // 获取用户购物车中的商品列表
            var content = db
                .User_shoppingCart.Where(x => x.account.Equals(account.account))
                .ToList()
                .Select(g => new
                {
                    active = g.count,
                    id = g.babys.id,
                    name = g.babys.name,
                    city = g.babys.city,
                    time = g.babys.time,
                    address = g.babys.address,
                    photo = g.babys.photo,
                    price = g.babys.price,
                    type = g.babys.type,
                    total = g.babys.total,
                    alreadyBuy = g.babys.alreadyBuy,
                    belongs = g.babys.belongs,
                    brand = g.babys.brand,
                    message = g.babys.message,
                    attribute = g.babys.attribute,
                    selectedStyle = g.type,
                });

            return Ok(content);
        }

        //添加订单 TODO 1. 商品数量正确添加，多订单不同商家同时看到订单。
        [HttpPost]
        [Route("addOrder")]
        public IHttpActionResult addOrder(Orders value)
        {
            string orderUUID = Guid.NewGuid().ToString();

            Orders order = new Orders
            {
                time = DateTime.Now,
                belong = value.belong,
                baby = value.baby,
                status = false,
                // 不再需要设置 orderNumber，因为它是 GUID 类型，会自动生成
            };

            Guid uuid = GenerateUUID(value.belong, value.baby, value.time);

            try
            {
                var sql =
                    "INSERT INTO Orders (belong, baby, time, status,refund,orderNumber) "
                    + "VALUES (@belong, @baby, @time, @status,'未开启',@orderNumber)";

                db.Database.ExecuteSqlCommand(
                    sql,
                    new SqlParameter("@belong", order.belong),
                    new SqlParameter("@baby", order.baby),
                    new SqlParameter("@time", order.time),
                    new SqlParameter("@status", order.status),
                    new SqlParameter("@orderNumber", uuid)
                );

                Orders res = db
                    .Orders.OrderByDescending(x => x.ID)
                    .FirstOrDefault(x => x.belong == order.belong);

                if (res != null)
                {
                    generateOrders(value.baby, res.ID);

                    return Ok(res);
                }

                return BadRequest("生成失败");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //手动生成UUID
        public static Guid GenerateUUID(string belong, string baby, DateTime time)
        {
            // 将 `belong`、`baby` 和 `time` 拼接为一个字符串
            string input = $"{belong}{baby}{time:O}"; // ISO 8601 格式

            // 使用 MD5 哈希生成稳定 UUID
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

                // 确保长度为 16 字节（128 位）并设置版本和变体
                hashBytes[6] = (byte)((hashBytes[6] & 0x0F) | 0x30); // 设置为版本 3
                hashBytes[8] = (byte)((hashBytes[8] & 0x3F) | 0x80); // 设置为 RFC 4122 变体

                return new Guid(hashBytes);
            }
        }

        //获取订单
        [HttpPost]
        [Route("GetOrders")]
        public IHttpActionResult GetOrders(middleTier value)
        {
            List<Orders> before = null;
            int total = 0;

            //Where 中类型强制绑定，无法自动装包，需手动转换类型
            Guid orderNumberGuid;
            bool isOrderNumberGuid = Guid.TryParse(value.query, out orderNumberGuid); // 尝试将 query 转换为 Guid

            if (value.id != 0 && isOrderNumberGuid)
            {
                before = new List<Orders>
                {
                    db.Orders.FirstOrDefault(x =>
                        x.ID.Equals(value.id) || x.orderNumber.Equals(orderNumberGuid)
                    )
                };
            }
            {
                Int32 Id;
                bool parse = int.TryParse(value.query, out Id);

                var temp = db
                    .Orders.Where(x => (x.time > value.startTime && x.time <= value.time))
                    .Where(x =>
                        (parse ? x.ID.Equals(Id) : value.query == null)
                        || (x.orderNumber.Equals(orderNumberGuid) || value.query == null)
                        || (x.baby.Contains(value.query) || value.query == null)
                        || (x.belong.Contains(value.query) || value.query == null)
                    ); // 确保时间逻辑正确

                // 分类
                if (value.mode != "true" && value.mode != "false" && value.mode != null)
                {
                    temp = temp.Where(c => c.refund == value.mode);
                }
                else
                {
                    temp = temp.Where(x =>
                        x.status.Equals(value.mode == "true") || value.mode == null
                    );
                }

                before = temp.OrderBy(x => x.time) // 按 时间 排序
                    .Skip((value.page - 1) * 10) // 分页跳过
                    .Take(10) // 取 10 条记录
                    .ToList();

                total = temp.Count();
            }

            return Ok(new { data = before, total = total });
        }

        //个人信息获取订单
        [HttpPost]
        [Route("GetOrderHistory")]
        public IHttpActionResult GetOrderHistory(middleTier value)
        {
            if (value == null || value.username == null)
                return BadRequest("请求异常");

            List<Orders> res = db
                .Orders.Where(x =>
                    x.belong.Equals(value.username)
                    && (
                        x.status == (value.type == "complete")
                        || x.status != (value.type == "wait")
                        || value.type == "All"
                    )
                )
                .OrderBy(g => g.ID)
                .Skip((value.page - 1) * 10)
                .Take(10)
                .ToList();

            if (res == null)
                return Ok(new { });

            return Ok(new { data = res, total = res.Count });
        }

        //个人信息展示
        [HttpPost]
        [Route("GetSpace")]
        public IHttpActionResult GetSpace(PlayerList value)
        {
            if (value == null)
                return BadRequest("异常请求");

            PlayerList space = db.PlayerList.FirstOrDefault(x => x.id.Equals(value.id));

            object res = new
            {
                name = space.name,
                gender = space.gender,
                phone = space.phone,
                email = space.email,
                DeliveryAddress = space.DeliveryAddress,
                birthday = space.birthday,
                hobbies = space.hobbies,
                income = space.income,
            };

            return Ok(res);
        }

        //个人信息开启退款
        [HttpPost]
        [Route("StratRefund")]
        public IHttpActionResult StratRefund(Orders value)
        {
            if (value is null)
            {
                return BadRequest("异常请求");
            }

            Orders order = db.Orders.FirstOrDefault(x => x.orderNumber == value.orderNumber);
            if (order == null)
                return BadRequest("未发现订单");

            if (order.handling_number == 0 || order.handling_number == 2)
            {
                order.refund = "待处理";
                order.handling_number += 1;
                return Ok(db.SaveChanges() > 0);
            }
            else
            {
                return BadRequest("无法再次操作");
            }
        }

        //个人信息取消退款
        [HttpPost]
        [Route("cancelRefund")]
        public IHttpActionResult cancelRefund(Orders value)
        {
            if (value is null)
            {
                return BadRequest("异常请求");
            }

            Orders order = db.Orders.FirstOrDefault(x => x.orderNumber == value.orderNumber);
            if (order == null)
                return BadRequest("未发现订单");

            if (order.handling_number == 1 || order.handling_number == 3)
            {
                order.refund = "未开启";
                order.handling_number -= 1;
                return Ok(db.SaveChanges() > 0);
            }
            else
            {
                return BadRequest("无法再次操作");
            }
        }

        //更新信息
        [HttpPost]
        [Route("UpdateSpace")]
        public IHttpActionResult UpdateSpace(PlayerList value)
        {
            if (value.id == null)
                return BadRequest("异常请求");

            PlayerList space = db.PlayerList.FirstOrDefault(x => x.id.Equals(value.id));

            space.email = value.email;
            space.gender = value.gender;
            space.birthday = value.birthday;
            space.hobbies = value.hobbies;
            space.phone = value.phone;
            space.income = value.income;
            space.DeliveryAddress = value.DeliveryAddress;

            return Ok(db.SaveChanges());
        }

        //商品详情页信息
        [HttpPost]
        [Route("GetDetail")]
        public IHttpActionResult GetDetail(babys value)
        {
            if (value.id < 0 || value.id == null)
                return BadRequest("请求异常");

            var res = db.babys.FirstOrDefault(x => x.id == value.id);

            if (res == null)
                return BadRequest("商品不存在");

            var details = db
                .DetailsPhoto.Where(x => x.Baby == value.id)
                .ToList() // Materialize the query to avoid translation issues
                .GroupBy(x => x.Types);

            object end = new
            {
                title = res.name,
                key = res.id,
                defaultPrice = res.price,
                brand = res.brand,
                address = res.address,
                attribute = res.attribute,
                merchants = res.belongs,
                message = res.message,
                defaultTotal = res.total,
                res = details
                    .Select(se => new
                    {
                        type = se.Key,
                        id = new { item = se.Select(z => z.position), id = se.Select(g => g.id) },
                        price = se.FirstOrDefault()?.price, // Use null-conditional operator
                        banner = se.Where(d => d.position.Equals("banner"))
                            .Select(g => g.Photo)
                            .ToList(),
                        show = se.Where(d => d.position.Equals("show"))
                            .Select(g => g.Photo)
                            .ToList(),
                        total = se.FirstOrDefault(g => g.position.Equals("total")).total,
                        buy = se.FirstOrDefault(g => g.position.Equals("total")).alreadyBuy == null
                            ? 0
                            : se.FirstOrDefault(g => g.position.Equals("total")).alreadyBuy,
                    })
                    .ToList(),
            };

            return Ok(end);
        }

        //上传头像
        [HttpPost]
        [Route("UploadAvatar")]
        public IHttpActionResult UploadAvatar()
        {
            try
            {
                // 确保请求包含文件
                if (HttpContext.Current.Request.Files.Count > 0)
                {
                    var uploadedFileNames = new List<string>();
                    var uploadPath = HttpContext.Current.Server.MapPath("~/avatar/");

                    // 确保文件夹存在
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // 处理所有上传的文件
                    for (int i = 0; i < HttpContext.Current.Request.Files.Count; i++)
                    {
                        var file = HttpContext.Current.Request.Files[i];

                        if (file != null && file.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                            var fullPath = Path.Combine(uploadPath, uniqueFileName);

                            // 保存文件到服务器
                            file.SaveAs(fullPath);
                            uploadedFileNames.Add(uniqueFileName);

                            int id = Convert.ToInt32(HttpContext.Current.Request.Form["id"]);

                            PlayerList res = db.PlayerList.FirstOrDefault(x => x.id.Equals(id));

                            res.avatar = uniqueFileName;

                            db.SaveChanges();
                        }
                    }

                    return Ok(uploadedFileNames);
                }
                else
                {
                    return BadRequest("No files found in the upload.");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        //获取头像
        [HttpPost]
        [Route("GetAvatar")]
        public IHttpActionResult GetAvatar(PlayerList value)
        {
            if (value == null)
                return BadRequest("异常请求");

            return Ok(db.PlayerList.FirstOrDefault(x => x.id.Equals(value.id)).avatar);
        }

        //商品详情页店家修改
        [HttpPost]
        [Route("merchantInfo")]
        public IHttpActionResult MerchantInfo([FromBody] middleTier value)
        {
            babys res = db.babys.FirstOrDefault(x =>
                x.id == value.id && x.belongs.Equals(value.belongs)
            );

            if (res == null)
                return BadRequest("请求异常");

            res.brand = value.brand;
            res.message = value.message;
            res.name = value.name;
            res.address = value.dress;
            res.brand = value.brand;

            return Ok(db.SaveChanges() > 0);
        }

        //商品详情页修改
        [HttpPost]
        [Route("UpdateDetail")]
        public IHttpActionResult UpdateDetail(middleTier value)
        {
            babys baby = db.babys.FirstOrDefault(x =>
                x.id == value.id && x.belongs.Equals(value.belongs)
            );

            if (baby == null)
                return BadRequest("请求异常");

            baby.price = value.defaultPrice;
            baby.total = value.defaultTotal;

            if (value.types != null)
            {
                for (var i = 0; i < value.types.Count; i++)
                {
                    string item = value.types[i];

                    DetailsPhoto res = db.DetailsPhoto.FirstOrDefault(g =>
                        g.Baby == value.id && g.Types == item && g.position == "total"
                    );

                    res.price = value.prices[i];
                    res.total = value.totals[i];

                    db.Entry(res).State = EntityState.Modified;
                }
            }

            return Ok(db.SaveChanges() > 0);
        }

        //详情页图片
        [HttpPost]
        [Route("DetailsPhoto")]
        public IHttpActionResult DetailsPhoto()
        {
            string type = HttpContext.Current.Request.Form["type"];

            switch (type)
            {
                case "":
                    type = "默认";
                    break;
            }

            string position = HttpContext.Current.Request.Form["position"];

            switch (position)
            {
                case "":
                    position = "banner";
                    break;
            }

            string belong = HttpContext.Current.Request.Form["belong"];

            int Baby = 0;

            try
            {
                Baby = Convert.ToInt32(HttpContext.Current.Request.Form["Baby"]);
            }
            catch (Exception e)
            {
                return BadRequest("异常请求");
            }

            try
            {
                // 确保请求包含文件
                if (HttpContext.Current.Request.Files.Count > 0)
                {
                    var uploadedFileNames = new List<string>();
                    var uploadPath = HttpContext.Current.Server.MapPath("~/UploadedFiles/");

                    // 确保文件夹存在
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // 处理所有上传的文件
                    for (int i = 0; i < HttpContext.Current.Request.Files.Count; i++)
                    {
                        var file = HttpContext.Current.Request.Files[i];

                        if (file != null && file.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                            var fullPath = Path.Combine(uploadPath, uniqueFileName);

                            // 保存文件到服务器
                            file.SaveAs(fullPath);
                            uploadedFileNames.Add(uniqueFileName);

                            if (type != "默认")
                            {
                                Models.DetailsPhoto res = new Models.DetailsPhoto();
                                res.Photo = uniqueFileName;
                                res.total = 0;
                                res.price = 0;
                                res.Types = type;
                                res.position = position;
                                res.belong = belong;
                                res.Baby = Baby;

                                db.DetailsPhoto.Add(res);
                            }
                            else
                            {
                                babys item = db.babys.Find(Baby);
                                item.photo = uniqueFileName;
                            }

                            db.SaveChanges();
                        }
                    }

                    return Ok(uploadedFileNames);
                }
                else
                {
                    return BadRequest("No files found in the upload.");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        //详情页添加款式
        [HttpPost]
        [Route("addType")]
        public IHttpActionResult addType([FromBody] middleTier value)
        {
            if (value.belongs == null || value.type == null || value.id == null)
                return BadRequest();
            DetailsPhoto total = new DetailsPhoto();
            total.Photo = "total";
            total.position = "total";
            total.Types = value.type;
            total.belong = value.belongs;
            total.Baby = value.id;
            total.alreadyBuy = 0;
            total.price = 1;
            total.total = 0;
            db.DetailsPhoto.Add(total);
            db.SaveChanges();

            return Ok(total);
        }

        //详情页删除款式
        [HttpPost]
        [Route("DeleteType")]
        public IHttpActionResult DeleteType([FromBody] DetailsPhoto value)
        {
            if (value.Types == null || value.belong == null || value.Baby == null)
                return BadRequest("请求异常");

            List<DetailsPhoto> res = db
                .DetailsPhoto.Where(x =>
                    x.Types == value.Types && x.belong == value.belong && x.Baby == value.Baby
                )
                .ToList();

            foreach (DetailsPhoto item in res)
            {
                db.DetailsPhoto.Remove(item);
            }

            return Ok(db.SaveChanges() > 0);
        }

        //TODO: 详情页系列的款式下单修复已购买数量的准确问题

        //图片删除
        [HttpPost]
        [Route("DeletePhoto")]
        public IHttpActionResult DeletePhoto([FromBody] DetailsPhoto value)
        {
            if (value.Photo == null)
                return BadRequest("请求异常");

            DetailsPhoto res = db.DetailsPhoto.FirstOrDefault(x =>
                x.Photo == value.Photo && x.position != "total"
            );

            db.DetailsPhoto.Remove(res);

            return Ok(db.SaveChanges() > 0);
        }

        //修改订单
        [HttpPost]
        [Route("UpdateOrder")]
        public IHttpActionResult UpdateOrder(Orders value)
        {
            //订单状态修改,现在没有付款验证api,后续完善
            if (value.ID == null || value.baby == null)
                BadRequest("请求异常");

            Orders @default = db.Orders.FirstOrDefault(x => x.ID == value.ID);

            @default.status = value.status;

            if (
                value.refund != null && @default.handling_number == 1
                || @default.handling_number == 3
            )
            {
                @default.refund = value.refund;
                @default.handling_number += 1;
            }

            if (@default.status)
            {
                List<User_shoppingCart> userShoppingCarts = db
                    .User_shoppingCart.Where(x => x.account == @default.belong)
                    .ToList();
                foreach (User_shoppingCart item in userShoppingCarts)
                {
                    db.User_shoppingCart.Remove(item);
                }
                db.SaveChanges();
            }

            return Ok(db.SaveChanges() > 0);
        }

        private void generateOrders(string babys, int order)
        {
            //正则拿取里面的所有ID
            Regex regex = new Regex(@"(\d+)\+\d+:[^;]+");
            MatchCollection matches = regex.Matches(babys);

            List<int> ids = new List<int>();
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    // 将 id 部分添加到 ids 列表中
                    ids.Add(int.Parse(match.Groups[1].Value));
                }
            }

            ArrayList belongs = new ArrayList();

            //根据id拿到里面的对应商家
            foreach (int id in ids)
            {
                string belong = db.babys.Find(id).belongs;
                belongs.Add(belong);
                shipped shipped = new shipped
                {
                    shop = belong,
                    baby = id,
                    logistics = "暂未发货",
                    bindOrder = order
                };
                db.shipped.Add(shipped);
            }

            db.SaveChanges();

            Console.WriteLine(belongs.Count);
        }

        //删除订单 TODO 后续多人订单的数据也跟着删除了
        [HttpPost]
        [Route("deleteOrder")]
        public IHttpActionResult DeleteOrder(Orders value)
        {
            if (value.ID == null)
                BadRequest("请求异常");

            Orders @default = db.Orders.FirstOrDefault(x => x.ID == value.ID);

            db.Orders.Remove(@default);

            return Ok(db.SaveChanges() > 0);
        }

        //管理员
        [HttpPost]
        [Route("AdminLogin")]
        public int AdminLogin([FromBody] Admin value)
        {
            Admin Res = db.Admin.FirstOrDefault(x =>
                x.account.Equals(value.account) && x.pwd.Equals(value.pwd)
            );
            return Res?.authority ?? 0;
        }

        //商家订单管理
        [HttpPost]
        [Route("ShopOrder")]
        public IHttpActionResult ShopOrder([FromBody] middleTier value)
        {
            if (value.belongs == null)
                return BadRequest("异常请求");
            List<dynamic> before = new List<dynamic>();
            int total = 0;

            Guid orderNumberGuid;
            bool isOrderNumberGuid = Guid.TryParse(value.query, out orderNumberGuid);

            if (value.id != 0 && isOrderNumberGuid)
            {
                var order = db.Orders.FirstOrDefault(x =>
                    x.ID.Equals(value.id)
                    || x.orderNumber.Equals(orderNumberGuid) && x.belong.Equals(value.belongs)
                );
                if (order != null)
                {
                    before.Add(
                        new
                        {
                            order.ID,
                            order.orderNumber,
                            order.baby,
                            order.time,
                            order.status,
                            order.logistics,
                            order.refund
                        }
                    );
                }
            }

            int id;
            bool parse = int.TryParse(value.query, out id);

            var temp = db.Orders.Where(x =>
                (x.time > value.startTime && x.time <= value.time)
                && (
                    (parse ? x.ID.Equals(id) : value.query == null)
                    || (x.orderNumber.Equals(orderNumberGuid) || value.query == null)
                    || (x.baby.Contains(value.query) || value.query == null)
                )
            );

            if (value.mode != "true" && value.mode != "false" && value.mode != null)
            {
                temp = temp.Where(c => c.refund == value.mode);
            }
            else
            {
                temp = temp.Where(x => x.status.Equals(value.mode == "true") || value.mode == null);
            }

            //商家检测,修复多订单，不同商家查看
            List<int> orderList = db
                .shipped.Where(p => p.shop.Equals(value.belongs))
                .Select(shipped => shipped.bindOrder)
                .ToList();

            if (orderList == null || orderList.Count == 0)
            {
                return BadRequest("没有订单");
            }

            foreach (int i in orderList)
            {
                temp = temp.Where(x => x.ID.Equals(i));
                var test = temp.ToList();
            }

            // 创建一个新的列表来存储结果,数组合并
            before.AddRange(
                temp.OrderBy(x => x.ID) // 按 ID 排序
                    .Skip((value.page - 1) * 10) // 分页
                    .Take(10) // 取 10 条记录
                    .ToList()
            );

            total = temp.Count();

            return Ok(new { data = before, total });
        }

        //商家多订单物流管理
        [HttpPost]
        [Route("multiplayerMode")]
        public IHttpActionResult multiplayerMode(middleTier value)
        {
            if (value == null)
                return BadRequest("异常请求");

            List<shipped> res = db
                .shipped.Where(x => x.bindOrder == value.id && x.shop == value.belongs)
                .ToList();

            return Ok(res);
        }

        //商家多订单发货
        [HttpPost]
        [Route("ShippedDelivery")]
        public IHttpActionResult ShippedDelivery(shipped value)
        {
            if (value == null)
                return BadRequest("异常请求");

            shipped res = db.shipped.Find(value.id);

            res.logistics = value.logistics;

            return Ok(db.SaveChanges() > 0);
        }

        //商家发货
        [HttpPost]
        [Route("OrderDelivery")]
        public IHttpActionResult OrderDelivery([FromBody] Orders value)
        {
            if (value.ID == null)
                return BadRequest("请求异常");

            Orders @default = db.Orders.FirstOrDefault(x => x.ID == value.ID);

            if (value.logistics == "")
            {
                @default.logistics = "未发货";
            }
            else
            {
                int Count = db.shipped.Where(p => p.bindOrder == value.ID).ToList().Count;

                if (Count < 2)
                {
                    @default.logistics = value.logistics;
                }
                else
                {
                    return BadRequest("多订单请发货前往详情页");
                }
            }

            if (db.SaveChanges() > 0)
            {
                return Ok("修改成功");
            }
            else
            {
                return BadRequest("修改失败");
            }
        }

        //商家发货商品查看 TODO 数据库给 shipped 添加表同订单号的所有商家均发货后，再将订单的发货状态修改
        [HttpPost]
        [Route("OrderBabyList")]
        public IHttpActionResult OrderDeliveryDetails([FromBody] middleTier value)
        {
            //TODO 修复此处多款式缺少异常
            if (value == null)
                return BadRequest("请求异常");

            List<dynamic> res = new List<dynamic>();

            foreach (var item in value.babys)
            {
                res.Add(
                    db.babys.FirstOrDefault(x =>
                        x.id.Equals(item)
                        && (value.belongs != null ? (x.belongs == value.belongs) : true)
                    )
                );
            }

            if (res[0] == null)
                res.RemoveAt(0);

            return Ok(res);
        }

        //商品管理
        [HttpPost]
        [Route("shopManager")]
        public List<babys> ShopManager([FromBody] Admin value)
        {
            return db.babys.Where(x => x.belongs.Equals(value.account)).ToList();
        }

        //商品管理
        [HttpPost]
        [Route("CustomerBaby")]
        public List<babys> CustomerBaby([FromBody] middleTier value)
        {
            BeforeDate = null;
            return db.babys.OrderBy(x => x.id).Skip((value.page - 1) * 10).Take(10).ToList();
        }

        //商品修改
        [HttpPost]
        [Route("ShopChange")]
        public bool ShopChange([FromBody] babys value)
        {
            var res = db.babys.FirstOrDefault(x => x.id == value.id);

            if (res != null)
            {
                res.name = value.name;
                res.time = value.time;
                res.price = value.price;
                res.total = value.total;
                res.address = value.address;
                res.type = value.type;
                res.city = value.city;
                res.photo = value.photo;
                res.attribute = value.attribute;
                db.Entry(res).State = EntityState.Modified;
                return db.SaveChanges() > 0;
            }

            return false;
        }

        //移除商品
        [HttpPost]
        [Route("DelShop")]
        public bool DelShop([FromBody] babys value)
        {
            var itemToDelete = db.babys.FirstOrDefault(b => b.id == value.id);

            if (itemToDelete != null)
            {
                db.babys.Remove(itemToDelete);
                db.SaveChanges();
                return true; // 删除成功
            }

            return false; // 未找到该商品
        }

        //添加商品
        [HttpPost]
        [Route("addBaby")]
        public IHttpActionResult AddBaby([FromBody] babys value)
        {
            try
            {
                db.babys.Add(value);

                return Ok(db.SaveChanges() > 0 ? "添加成功" : "商品已存在");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest("请求异常");
            }
        }

        //添加预览图
        [HttpPost]
        [Route("upLoadPhoto")]
        public IHttpActionResult UpLoadPhoto()
        {
            try
            {
                // 检查请求中是否包含文件
                if (HttpContext.Current.Request.Files.Count > 0)
                {
                    var uploadedFileNames = new List<string>();
                    var uploadPath = HttpContext.Current.Server.MapPath("~/UploadedFiles/");

                    // 确保保存路径存在
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // 遍历所有上传的文件
                    for (int i = 0; i < HttpContext.Current.Request.Files.Count; i++)
                    {
                        var file = HttpContext.Current.Request.Files[i];

                        if (file != null && file.ContentLength > 0)
                        {
                            // 获取文件名并生成唯一文件名
                            var fileName = Path.GetFileName(file.FileName);
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;

                            // 文件保存路径
                            var fullPath = Path.Combine(uploadPath, uniqueFileName);

                            // 保存文件
                            file.SaveAs(fullPath);

                            // 添加成功保存的文件名到列表
                            uploadedFileNames.Add(uniqueFileName);
                        }
                    }

                    // 返回所有成功保存的文件名
                    return Ok("文件上传成功: " + string.Join(", ", uploadedFileNames));
                }
                else
                {
                    return BadRequest("未找到文件上传");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        //账号管理
        [HttpPost]
        [Route("shopUser")]
        public IHttpActionResult ShopUser([FromBody] middleTier value)
        {
            if (value.id != null && value.pwd != null)
            {
                db.Admin.FirstOrDefault(x => x.Id == value.id).pwd = value.pwd;
                return Ok((db.SaveChanges() > 0) ? "修改成功" : "修改失败");
            }

            var res = db
                .Admin.Where(x =>
                    x.authority == 3 & (value.query == null || x.account.Contains(value.query))
                )
                .OrderBy(x => x.account);
            return Ok(
                new { res = res.Skip((value.page - 1) * 10).Take(10).ToList(), total = res.Count() }
            );
        }
    }
}
