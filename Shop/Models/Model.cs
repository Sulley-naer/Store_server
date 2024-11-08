using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;

    public class Model : DbContext
    {
        public Model()
            : base("name=Entities")
        {
            Database.SetInitializer<Model>(null);
        }

        public DbSet<PlayerList> PlayerList { get; set; }
        public DbSet<babys> babys { get; set; }
        public DbSet<User_shoppingCart> User_shoppingCart { get; set; }
        public DbSet<Admin> Admin { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<DetailsPhoto> DetailsPhoto { get; set; }
    }
}