using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;

namespace TransactionEF
{
    class Program
    {
        public static void Main()
        {
            var connectionString = @"Data Source=DESKTOP-1QNDD3N;Initial Catalog=Blogging;Integrated Security=False;Persist Security Info=False;User ID=sa;Password=sapassword";

            //using (var context = new BloggingContext(
            //    new DbContextOptionsBuilder<BloggingContext>()
            //        .UseSqlServer(connectionString)
            //        .Options))
            //{
            //    context.Database.EnsureDeleted();
            //    context.Database.EnsureCreated();
            //}

            #region Transaction
            var options = new DbContextOptionsBuilder<BloggingContext>()
                .UseSqlServer(new SqlConnection(connectionString))
                .Options;

            using (var context1 = new BloggingContext(options))
            {
                using (var transaction = context1.Database.BeginTransaction())
                {
                    try
                    {
                        var blogs = new List<Blog>() {
                            new Blog { Url = "http://blogs.msdn.com/dotnet" },
                            new Blog { Url = "http://blogs.msdn.com/dotnet/2" }
                        };
                        context1.Blogs.AddRange(blogs);
                        context1.SaveChanges();

                        using (var context2 = new BloggingContext(options))
                        {
                            context2.Database.UseTransaction(transaction.GetDbTransaction());

                            var blogList = context2.Blogs
                                .OrderBy(b => b.Id)
                                .ToList();
                            foreach (Blog blog in blogList.ToList())
                            {
                                Console.WriteLine(blog.Id + blog.Url);
                            }
                        }

                        // Commit transaction if all commands succeed, transaction will auto-rollback
                        // when disposed if either commands fails
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        // TODO: Handle failure
                        Console.WriteLine(e.Message);
                    }
                }
            }
            #endregion

            Console.ReadKey();
        }

        public class BloggingContext : DbContext
        {
            public BloggingContext(DbContextOptions<BloggingContext> options)
                : base(options)
            { }

            public DbSet<Blog> Blogs { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.ApplyConfiguration(new OrderConfiguration());
            }
        }

        public class Blog
        {
            public int Id { get; set; }
            public string Url { get; set; }
        }

        public class OrderConfiguration : IEntityTypeConfiguration<Blog>
        {
            public void Configure(EntityTypeBuilder<Blog> builder)
            {
                builder.ToTable(nameof(Blog));
                builder.HasKey(o => o.Id);
            }
        }
    }
}
