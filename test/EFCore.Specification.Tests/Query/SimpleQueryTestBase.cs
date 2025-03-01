﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.EntityFrameworkCore
{
    public abstract class SimpleQueryTestBase : NonSharedModelTestBase
    {
        public static IEnumerable<object[]> IsAsyncData = new[] { new object[] { false }, new object[] { true } };

        protected override string StoreName => "SimpleQueryTests";

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Multiple_nested_reference_navigations(bool async)
        {
            var contextFactory = await InitializeAsync<Context24368>();
            using var context = contextFactory.CreateContext();
            var id = 1;
            var staff = await context.Staff.FindAsync(3);

            Assert.Equal(1, staff.ManagerId);

            var query = context.Appraisals
                    .Include(ap => ap.Staff).ThenInclude(s => s.Manager)
                    .Include(ap => ap.Staff).ThenInclude(s => s.SecondaryManager)
                    .Where(ap => ap.Id == id);

            var appraisal = async
                ? await query.SingleOrDefaultAsync()
                : query.SingleOrDefault();

            Assert.Equal(1, staff.ManagerId);

            Assert.NotNull(appraisal);
            Assert.Same(staff, appraisal.Staff);
            Assert.NotNull(appraisal.Staff.Manager);
            Assert.Equal(1, appraisal.Staff.ManagerId);
            Assert.NotNull(appraisal.Staff.SecondaryManager);
            Assert.Equal(2, appraisal.Staff.SecondaryManagerId);
        }

        protected class Context24368 : DbContext
        {
            public Context24368(DbContextOptions options)
                   : base(options)
            {
            }

            public DbSet<Appraisal> Appraisals { get; set; }
            public DbSet<Staff> Staff { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Staff>().HasIndex(e => e.ManagerId).IsUnique(false);
                modelBuilder.Entity<Staff>()
                    .HasOne(a => a.Manager)
                    .WithOne()
                    .HasForeignKey<Staff>(s => s.ManagerId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);

                modelBuilder.Entity<Staff>().HasIndex(e => e.SecondaryManagerId).IsUnique(false);
                modelBuilder.Entity<Staff>()
                    .HasOne(a => a.SecondaryManager)
                    .WithOne()
                    .HasForeignKey<Staff>(s => s.SecondaryManagerId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);

                modelBuilder.Entity<Staff>().HasData(
                    new Staff { Id = 1, Email = "mgr1@company.com", Logon = "mgr1", Name = "Manager 1" },
                    new Staff { Id = 2, Email = "mgr2@company.com", Logon = "mgr2", Name = "Manager 2", ManagerId = 1 },
                    new Staff { Id = 3, Email = "emp@company.com", Logon = "emp", Name = "Employee", ManagerId = 1, SecondaryManagerId = 2 }
                );

                modelBuilder.Entity<Appraisal>().HasData(new Appraisal()
                {
                    Id = 1,
                    PeriodStart = new DateTimeOffset(new DateTime(2020, 1, 1).ToUniversalTime()),
                    PeriodEnd = new DateTimeOffset(new DateTime(2020, 12, 31).ToUniversalTime()),
                    StaffId = 3
                });
            }
        }

        protected class Appraisal
        {
            public int Id { get; set; }

            public int StaffId { get; set; }
            public Staff Staff { get; set; }

            public DateTimeOffset PeriodStart { get; set; }
            public DateTimeOffset PeriodEnd { get; set; }

            public bool Complete { get; set; }
            public bool Deleted { get; set; }
        }

        protected class Staff
        {
            public int Id { get; set; }
            [MaxLength(100)]
            public string Logon { get; set; }
            [MaxLength(150)]
            public string Email { get; set; }
            [MaxLength(100)]
            public string Name { get; set; }

            public int? ManagerId { get; set; }
            public Staff Manager { get; set; }

            public int? SecondaryManagerId { get; set; }
            public Staff SecondaryManager { get; set; }
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Comparing_enum_casted_to_byte_with_int_parameter(bool async)
        {
            var contextFactory = await InitializeAsync<Context21770>();
            using var context = contextFactory.CreateContext();
            var bitterTaste = Taste.Bitter;
            var query = context.IceCreams.Where(i => i.Taste == (byte)bitterTaste);

            var bitterIceCreams = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Single(bitterIceCreams);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Comparing_enum_casted_to_byte_with_int_constant(bool async)
        {
            var contextFactory = await InitializeAsync<Context21770>();
            using var context = contextFactory.CreateContext();
            var query = context.IceCreams.Where(i => i.Taste == (byte)Taste.Bitter);

            var bitterIceCreams = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Single(bitterIceCreams);
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Comparing_byte_column_to_enum_in_vb_creating_double_cast(bool async)
        {
            var contextFactory = await InitializeAsync<Context21770>();
            using var context = contextFactory.CreateContext();
            Expression<Func<Food, byte?>> memberAccess = (Food i) => i.Taste;
            var predicate = Expression.Lambda<Func<Food, bool>>(
                Expression.Equal(
                    Expression.Convert(memberAccess.Body, typeof(int?)),
                    Expression.Convert(
                        Expression.Convert(Expression.Constant(Taste.Bitter, typeof(Taste)), typeof(int)),
                        typeof(int?))),
                memberAccess.Parameters);
            var query = context.Food.Where(predicate);

            var bitterFood = async
                ? await query.ToListAsync()
                : query.ToList();
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Null_check_removal_in_ternary_maintain_appropriate_cast(bool async)
        {
            var contextFactory = await InitializeAsync<Context21770>();
            using var context = contextFactory.CreateContext();

            var query = from f in context.Food
                        select new
                        {
                            Bar = f.Taste != null ? (Taste)f.Taste : (Taste?)null
                        };

            var bitterFood = async
                ? await query.ToListAsync()
                : query.ToList();
        }

        protected class Context21770 : DbContext
        {
            public Context21770(DbContextOptions options)
                   : base(options)
            {
            }

            public DbSet<IceCream> IceCreams { get; set; }
            public DbSet<Food> Food { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<IceCream>(
                   entity =>
                   {
                       entity.HasData(
                           new IceCream { IceCreamId = 1, Name = "Vanilla", Taste = (byte)Taste.Sweet },
                           new IceCream { IceCreamId = 2, Name = "Chocolate", Taste = (byte)Taste.Sweet },
                           new IceCream { IceCreamId = 3, Name = "Match", Taste = (byte)Taste.Bitter });
                   });

                modelBuilder.Entity<Food>(
                    entity =>
                    {
                        entity.HasData(new Food { Id = 1, Taste = null });
                    });
            }
        }

        protected enum Taste : byte
        {
            Sweet = 0,
            Bitter = 1,
        }

        protected class IceCream
        {
            public int IceCreamId { get; set; }
            public string Name { get; set; }
            public int Taste { get; set; }
        }

        protected class Food
        {
            public int Id { get; set; }
            public byte? Taste { get; set; }
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Bool_discriminator_column_works(bool async)
        {
            var contextFactory = await InitializeAsync<Context24657>(seed: c => c.Seed());
            using var context = contextFactory.CreateContext();

            var query = context.Authors.Include(e => e.Blog);

            var authors = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(2, authors.Count);
        }

        protected class Context24657 : DbContext
        {
            public Context24657(DbContextOptions options)
                   : base(options)
            {
            }

            public DbSet<Author> Authors { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Blog>()
                    .HasDiscriminator<bool>(nameof(Blog.IsPhotoBlog))
                    .HasValue<DevBlog>(false)
                    .HasValue<PhotoBlog>(true);
            }

            public void Seed()
            {
                Add(new Author
                {
                    Blog = new DevBlog
                    {
                        Title = "Dev Blog",
                    }
                });
                Add(new Author
                {
                    Blog = new PhotoBlog
                    {
                        Title = "Photo Blog",
                    }
                });

                SaveChanges();
            }
        }

        protected class Author
        {
            public int Id { get; set; }
            public Blog Blog { get; set; }
        }

        protected abstract class Blog
        {
            public int Id { get; set; }
            public bool IsPhotoBlog { get; set; }
            public string Title { get; set; }
        }

        protected class DevBlog : Blog
        {
            public DevBlog()
            {
                IsPhotoBlog = false;
            }
        }

        protected class PhotoBlog : Blog
        {
            public PhotoBlog()
            {
                IsPhotoBlog = true;
            }

            public int NumberOfPhotos { get; set; }
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Count_member_over_IReadOnlyCollection_works(bool async)
        {
            var contextFactory = await InitializeAsync<Context26433>(seed: c => c.Seed());
            using var context = contextFactory.CreateContext();

            var query = context.Authors
                    .Select(a => new
                    {
                        BooksCount = a.Books.Count
                    });

            var authors = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(3, Assert.Single(authors).BooksCount);
        }

        protected class Context26433 : DbContext
        {
            public Context26433(DbContextOptions options)
                : base(options)
            {
            }

            public DbSet<Book26433> Books { get; set; }
            public DbSet<Author26433> Authors { get; set; }

            public void Seed()
            {

                base.Add(new Author26433
                    {
                        FirstName = "William",
                        LastName = "Shakespeare",
                        Books = new List<Book26433>
                        {
                            new() {Title = "Hamlet"},
                            new() {Title = "Othello"},
                            new() {Title = "MacBeth"}
                        }
                    });

                SaveChanges();
            }
        }

        protected class Author26433
        {
            [Key]
            public int AuthorId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public IReadOnlyCollection<Book26433> Books { get; set; }
        }

        protected class Book26433
        {
            [Key]
            public int BookId { get; set; }
            public string Title { get; set; }
            public int AuthorId { get; set; }
            public Author26433 Author { get; set; }
        }

#nullable enable

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task IsDeleted_query_filter_with_conversion_to_int_works(bool async)
        {
            var contextFactory = await InitializeAsync<Context26428>(seed: c => c.Seed());
            using var context = contextFactory.CreateContext();

            var query = context.Suppliers.Include(s => s.Location).OrderBy(s => s.Name);

            var suppliers = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(4, suppliers.Count);
            Assert.Single(suppliers.Where(e => e.Location != null));
        }

        protected class Context26428 : DbContext
        {
            public Context26428(DbContextOptions options)
                : base(options)
            {
            }

            public DbSet<Supplier> Suppliers => Set<Supplier>();
            public DbSet<Location> Locations => Set<Location>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Supplier>().Property(s => s.IsDeleted).HasConversion<int>();
                modelBuilder.Entity<Supplier>().HasQueryFilter(s => !s.IsDeleted);

                modelBuilder.Entity<Location>().Property(l => l.IsDeleted).HasConversion<int>();
                modelBuilder.Entity<Location>().HasQueryFilter(l => !l.IsDeleted);
            }

            public void Seed()
            {
                var activeAddress = new Location
                {
                    Address = "Active address",
                    IsDeleted = false
                };
                var deletedAddress = new Location
                {
                    Address = "Deleted address",
                    IsDeleted = true
                };

                var activeSupplier1 = new Supplier
                {
                    Name = "Active supplier 1",
                    IsDeleted = false,
                    Location = activeAddress
                };
                var activeSupplier2 = new Supplier
                {
                    Name = "Active supplier 2",
                    IsDeleted = false,
                    Location = deletedAddress
                };
                var activeSupplier3 = new Supplier
                {
                    Name = "Active supplier 3",
                    IsDeleted = false
                };
                var deletedSupplier = new Supplier
                {
                    Name = "Deleted supplier",
                    IsDeleted = false
                };

                AddRange(activeAddress, deletedAddress);
                AddRange(activeSupplier1, activeSupplier2, activeSupplier3, deletedSupplier);

                SaveChanges();
            }
        }

        protected class Supplier
        {
            public Guid SupplierId { get; set; }
            public string Name { get; set; } = null!;
            public Location? Location { get; set; }
            public bool IsDeleted { get; set; }
        }

        protected class Location
        {
            public Guid LocationId { get; set; }
            public string Address { get; set; } = null!;
            public bool IsDeleted { get; set; }
        }

#nullable disable

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery(bool async)
        {
            var contextFactory = await InitializeAsync<Context26593>(seed: c => c.Seed());
            using var context = contextFactory.CreateContext();

            var currentUserId = 1;

            var currentUserGroupIds = context.Memberships
                .Where(m => m.UserId == currentUserId)
                .Select(m => m.GroupId);

            var hasMembership = context.Memberships
                .Where(m => currentUserGroupIds.Contains(m.GroupId))
                .Select(m => m.User);

            var query = context.Users
                .Select(u => new
                {
                    HasAccess = hasMembership.Contains(u)
                });

            var users = async
                ? await query.ToListAsync()
                : query.ToList();
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_2(bool async)
        {
            var contextFactory = await InitializeAsync<Context26593>(seed: c => c.Seed());
            using var context = contextFactory.CreateContext();

            var currentUserId = 1;

            var currentUserGroupIds = context.Memberships
                .Where(m => m.UserId == currentUserId)
                .Select(m => m.Group);

            var hasMembership = context.Memberships
                .Where(m => currentUserGroupIds.Contains(m.Group))
                .Select(m => m.User);

            var query = context.Users
                .Select(u => new
                {
                    HasAccess = hasMembership.Contains(u)
                });

            var users = async
                ? await query.ToListAsync()
                : query.ToList();
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Unwrap_convert_node_over_projection_when_translating_contains_over_subquery_3(bool async)
        {
            var contextFactory = await InitializeAsync<Context26593>(seed: c => c.Seed());
            using var context = contextFactory.CreateContext();

            var currentUserId = 1;

            var currentUserGroupIds = context.Memberships
                .Where(m => m.UserId == currentUserId)
                .Select(m => m.GroupId);

            var hasMembership = context.Memberships
                .Where(m => currentUserGroupIds.Contains(m.GroupId))
                .Select(m => m.User);

            var query = context.Users
                .Select(u => new
                {
                    HasAccess = hasMembership.Any(e => e == u)
                });

            var users = async
                ? await query.ToListAsync()
                : query.ToList();
        }

        protected class Context26593 : DbContext
        {
            public Context26593(DbContextOptions options)
                : base(options)
            {
            }

            public DbSet<User> Users { get; set; }
            public DbSet<Group> Groups { get; set; }
            public DbSet<Membership> Memberships { get; set; }

            public void Seed()
            {
                var user = new User();
                var group = new Group();
                var membership = new Membership { Group = group, User = user };
                AddRange(user, group, membership);

                SaveChanges();
            }
        }

        protected class User
        {
            public int Id { get; set; }

            public ICollection<Membership> Memberships { get; set; }
        }

        protected class Group
        {
            public int Id { get; set; }

            public ICollection<Membership> Memberships { get; set; }
        }

        protected class Membership
        {
            public int Id { get; set; }
            public User User { get; set; }
            public int UserId { get; set; }
            public Group Group { get; set; }
            public int GroupId { get; set; }
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task GroupBy_aggregate_on_right_side_of_join(bool async)
        {
            var contextFactory = await InitializeAsync<Context26587>();
            using var context = contextFactory.CreateContext();

            var orderId = 123456;

            var orderItems = context.OrderItems.Where(o => o.OrderId == orderId);
            var items = orderItems
                .GroupBy(o => o.OrderId, (o, g) => new
                {
                    Key = o,
                    IsPending = g.Max(y => y.ShippingDate == null && y.CancellationDate == null ? o : (o - 10000000))
                })
                .OrderBy(e => e.Key);


            var query = orderItems
                .Join(items, x => x.OrderId, x => x.Key, (x, y) => x)
                .OrderBy(x => x.OrderId);


            var users = async
                ? await query.ToListAsync()
                : query.ToList();
        }

        protected class Context26587 : DbContext
        {
            public Context26587(DbContextOptions options)
                : base(options)
            {
            }

            public DbSet<OrderItem> OrderItems { get; set; }
        }

        protected class OrderItem
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
            public DateTime? ShippingDate { get; set; }
            public DateTime? CancellationDate { get; set; }
        }
    }
}
