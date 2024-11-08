﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
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

        //添加订单
        [HttpPost]
        [Route("addOrder")]
        public IHttpActionResult addOrder(Orders value)
        {
            Orders order = new Orders
            {
                time = DateTime.Now,
                belong = value.belong,
                baby = value.baby,
                status = false
                // 不再需要设置 orderNumber，因为它是 GUID 类型，会自动生成
            };

            var sql =
                "INSERT INTO Orders (belong, baby, time, status) "
                + "VALUES (@belong, @baby, @time, @status)";

            try
            {
                var result = db.Database.ExecuteSqlCommand(
                    sql,
                    new SqlParameter("@belong", order.belong),
                    new SqlParameter("@baby", order.baby),
                    new SqlParameter("@time", order.time),
                    new SqlParameter("@status", order.status)
                );

                return Ok(
                    db.Orders.OrderByDescending(x => x.ID)
                        .FirstOrDefault(x => x.belong == order.belong)
                );
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
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
                    .Where(x => x.status.Equals(value.mode == "true") || value.mode == null)
                    .Where(x =>
                        (parse ? x.ID.Equals(Id) : value.query == null)
                        || (x.orderNumber.Equals(orderNumberGuid) || value.query == null)
                        || (x.baby.Contains(value.query) || value.query == null)
                        || (x.belong.Contains(value.query) || value.query == null)
                    ); // 确保时间逻辑正确

                before = temp.OrderBy(x => x.ID) // 按 ID 排序
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
                .Orders.Where(x => x.belong.Equals(value.username))
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

            return Ok(
                new
                {
                    name = space.name,
                    gender = space.gender,
                    phone = space.phone,
                    email = space.email,
                    DeliveryAddress = space.DeliveryAddress,
                    birthday = space.birthday,
                    hobbies = space.hobbies,
                    income = space.income,
                }
            );
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
                    .Select(x => new
                    {
                        type = x.Key,
                        id = new { item = x.Select(z => z.position), id = x.Select(g => g.id) },
                        price = x.FirstOrDefault()?.price, // Use null-conditional operator
                        banner = x.Where(d => d.position.Equals("banner"))
                            .Select(g => g.Photo)
                            .ToList(),
                        show = x.Where(d => d.position.Equals("show"))
                            .Select(g => g.Photo)
                            .ToList(),
                        total = x.FirstOrDefault(g => g.position.Equals("total")).total == null
                            ? 0
                            : x.FirstOrDefault(g => g.position.Equals("total")).total,
                        buy = x.FirstOrDefault(g => g.position.Equals("total")).alreadyBuy == null
                            ? 0
                            : x.FirstOrDefault(g => g.position.Equals("total")).alreadyBuy,
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

                            Models.DetailsPhoto res = new Models.DetailsPhoto();
                            res.Photo = uniqueFileName;
                            res.total = 0;
                            res.price = 0;
                            res.Types = type;
                            res.position = position;
                            res.belong = belong;
                            res.Baby = Baby;

                            db.DetailsPhoto.Add(res);
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

        //TODO 详情页系列的款式下单修复已购买数量的准确问题

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
            if (value.ID == null)
                BadRequest("请求异常");

            Orders @default = db.Orders.FirstOrDefault(x => x.ID == value.ID);

            @default.status = value.status;

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

        //删除订单
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
        [Route("shopOrder")]
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
                            order.logistics
                        }
                    );
                }
            }

            int Id;
            bool parse = int.TryParse(value.query, out Id);

            var temp = db
                .Orders.Where(x =>
                    (x.time > value.startTime && x.time <= value.time)
                    && (x.status.Equals(value.mode == "true") || value.mode == null)
                    && x.belong.Equals(value.belongs)
                    && (
                        (parse ? x.ID.Equals(Id) : value.query == null)
                        || (x.orderNumber.Equals(orderNumberGuid) || value.query == null)
                        || (x.baby.Contains(value.query) || value.query == null)
                    )
                )
                .Select(x => new
                {
                    x.ID,
                    x.orderNumber,
                    x.baby,
                    x.time,
                    x.status,
                    x.logistics
                }); // 返回 ID orderNumber baby

            // 创建一个新的列表来存储结果
            before.AddRange(
                temp.OrderBy(x => x.ID) // 按 ID 排序
                    .Skip((value.page - 1) * 10) // 分页
                    .Take(10) // 取 10 条记录
                    .ToList()
            );

            total = temp.Count();

            ArrayList babyItems = new ArrayList();

            foreach (var order in before)
            {
                var items = order.baby.Split(';'); // 按照分号分割

                foreach (var item in items)
                {
                    var parts = item.Split('+');
                    if (parts.Length == 2)
                    {
                        babyItems.Add(new { baby = parts[0], sum = parts[1] });
                    }
                }
            }

            return Ok(
                new
                {
                    data = before,
                    babys = babyItems.ToArray(),
                    total
                }
            );
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
                @default.logistics = value.logistics;
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

        //商家发货商品查看
        [HttpPost]
        [Route("OrderBabyList")]
        public IHttpActionResult OrderDeliveryDetails([FromBody] middleTier value)
        {
            //TODO 修复此处多款式缺少异常
            if (value.babys == null)
                return BadRequest("请求异常");

            List<dynamic> res = new List<dynamic>();

            foreach (var item in value.babys)
            {
                res.Add(db.babys.FirstOrDefault(x => x.id.Equals(item)));
            }

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
