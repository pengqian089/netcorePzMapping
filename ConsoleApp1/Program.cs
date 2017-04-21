
using System.Data.SqlClient;
using CoreServiceLibrary;
using CoreServiceLibrary.Mapping;

namespace Console.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var connection =
                new SqlConnection("Data Source=.\\sqlexpress;Initial Catalog=yhpc;Integrated Security=True"))
            {
                connection.Open();


                //var list = connection.GetListPage<User>(1, 20, x => x.Age > 18 && x.Age < 30, new Sort { Ascending = false, PropertyName = "id" });
                //var l = connection.GetListPage<User>(2, 3, x => x.Name != "", new Sort { Ascending = false, PropertyName = "id" });

                //var list = connection.GetList<User>();

                //for (var i = 0; i < 1000; i++)
                //{
                //    var user = new User {Age = i, Description = "这个是描述" + i, Name = "name" + i};
                //    connection.Insert(user);
                //}
                //var u = connection.Get<User>(x => x.UserId == 1);
                //u.Age = 18;
                //u.Name = "fenglian";
                //u.Description = "石碑";
                //connection.Update(u);

                //var a = connection.Delete<User>(x => x.Age > 80 && x.Age <= 100);

                var user = new User { Age = 20, Description = "貌美如花的死胖子，大帅比死胖子。", Name = "火系魔导师胖子"};
                connection.Insert(user);

                user.Description = "胖子魔法体系最伟大贡献者";
                var a = connection.Update(user);

                connection.Close();
            }
            System.Console.ReadLine();
        }
    }



    public sealed class User : MappingModel<User>
    {


        public User()
        {
            Table("T_Users");

            Map(x => x.UserId).Key();
            Map(x => x.UserId).SetIdentity();
            Map(x => x.UserId).ReadOnly();

            Map(x => x.Name).Column("UserName");
            Map(x => x.UserId).Column("Id");
            Map(x => x.Sid).Ignore();

            Name = "pengqian";
        }

        public int UserId { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Description { get; set; }


        public int Sid { get; set; }
    }
}