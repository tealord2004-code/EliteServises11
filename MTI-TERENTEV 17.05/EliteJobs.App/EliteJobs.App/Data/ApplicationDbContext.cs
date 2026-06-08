using EliteJobs.App.Models;
using EliteJobs.App.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EliteJobs.App.Services;

namespace EliteJobs.App.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<AdminAccessKey> AdminAccessKeys { get; set; }
        public DbSet<PaymentRequest> PaymentRequests { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<OrderRequest> OrderRequests { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<DirectoryGroup> DirectoryGroups { get; set; }
        public DbSet<DirectoryItem> DirectoryItems { get; set; }
        public DbSet<CompetitorData> CompetitorData { get; set; }
        public DbSet<CompetitorBenchmark> CompetitorBenchmarks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentRequest>()
    .HasIndex(p => p.UserId)
    .HasDatabaseName("IX_PaymentRequests_UserId");
            modelBuilder.Entity<PaymentRequest>()
                .HasIndex(p => p.Status)
                .HasDatabaseName("IX_PaymentRequests_Status");


            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Subscription>()
    .HasIndex(s => s.UserId)
    .IsUnique()
    .HasDatabaseName("IX_Subscriptions_UserId");
            // Переименовываем таблицы Identity
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

            // Настройка индексов для Service
            modelBuilder.Entity<Service>()
                .HasIndex(s => s.Title)
                .HasDatabaseName("IX_Services_Title");
            modelBuilder.Entity<Service>()
                .HasIndex(s => s.Category)
                .HasDatabaseName("IX_Services_Category");
            modelBuilder.Entity<Service>()
                .HasIndex(s => s.City)
                .HasDatabaseName("IX_Services_City");
            modelBuilder.Entity<Service>()
                .HasIndex(s => s.PostedDate)
                .HasDatabaseName("IX_Services_PostedDate");

            // Связь с пользователем
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Provider)
                .WithMany(u => u.PostedServices)
                .HasForeignKey(s => s.ProviderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Связи для заказов
            modelBuilder.Entity<OrderRequest>()
                .HasOne(o => o.Service)
                .WithMany()
                .HasForeignKey(o => o.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrderRequest>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrderRequest>()
                .HasIndex(o => new { o.CustomerId, o.ServiceId })
                .IsUnique()
                .HasDatabaseName("IX_OrderRequests_Customer_Service");

            // Связи для чатов
            modelBuilder.Entity<ChatRoom>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChatRoom>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChatRoom>()
                .HasIndex(c => new { c.User1Id, c.User2Id })
                .IsUnique()
                .HasDatabaseName("IX_ChatRooms_Users");

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.ChatRoom)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => m.SentAt)
                .HasDatabaseName("IX_ChatMessages_SentAt");

            // Компании
            modelBuilder.Entity<Company>()
                .HasOne(c => c.Employer)
                .WithMany()
                .HasForeignKey(c => c.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Company>()
                .HasIndex(c => c.EmployerId)
                .IsUnique()
                .HasDatabaseName("IX_Companies_EmployerId");

            // Резюме
            modelBuilder.Entity<Resume>()
                .HasOne(r => r.Worker)
                .WithMany()
                .HasForeignKey(r => r.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Resume>()
                .HasIndex(r => r.WorkerId)
                .HasDatabaseName("IX_Resumes_WorkerId");

            // Заполняем роли
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "2", Name = "ProviderIndividual", NormalizedName = "PROVIDERINDIVIDUAL" },
                new IdentityRole { Id = "3", Name = "ProviderCompany", NormalizedName = "PROVIDERCOMPANY" },
                new IdentityRole { Id = "4", Name = "CustomerIndividual", NormalizedName = "CUSTOMERINDIVIDUAL" },
                new IdentityRole { Id = "5", Name = "CustomerCompany", NormalizedName = "CUSTOMERCOMPANY" }
            );

            // Администратор
            var adminUser = new ApplicationUser
            {
                Id = "admin-001",
                UserName = "admin@elitejobs.ru",
                NormalizedUserName = "ADMIN@ELITEJOBS.RU",
                Email = "admin@elitejobs.ru",
                NormalizedEmail = "ADMIN@ELITEJOBS.RU",
                EmailConfirmed = true,
                FirstName = "Администратор",
                LastName = "Системы",
                RegisteredDate = DateTime.UtcNow,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
            modelBuilder.Entity<ApplicationUser>().HasData(adminUser);
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = "admin-001", RoleId = "1" }
            );

            // Тестовые услуги
            modelBuilder.Entity<Service>().HasData(
                new Service
                {
                    Id = 1,
                    Title = "Разработка сайтов на .NET",
                    Category = "IT и разработка",
                    Price = "от 150 000 ₽",
                    Description = "Создание веб-приложений любой сложности. Опыт 10 лет.",
                    City = "Москва",
                    ProviderName = "Александр К.",
                    ProviderType = "Физ. лицо",
                    Contacts = "alex@email.com",
                    PostedDate = DateTime.UtcNow.AddDays(-7),
                    IsActive = true
                },
                new Service
                {
                    Id = 2,
                    Title = "DevOps консалтинг",
                    Category = "IT и разработка",
                    Price = "5 000 ₽/час",
                    Description = "Настройка CI/CD, Kubernetes, Docker. Выезд к клиенту.",
                    City = "Санкт-Петербург",
                    ProviderName = "CloudTech Solutions",
                    ProviderType = "Юр. лицо",
                    Contacts = "info@cloudtech.ru",
                    PostedDate = DateTime.UtcNow.AddDays(-3),
                    IsActive = true
                },
                new Service
                {
                    Id = 3,
                    Title = "Ремонт квартир под ключ",
                    Category = "Строительство и ремонт",
                    Price = "от 10 000 ₽/м²",
                    Description = "Капитальный ремонт с материалами. Гарантия 3 года.",
                    City = "Москва",
                    ProviderName = "СтройПрофи",
                    ProviderType = "Юр. лицо",
                    Contacts = "+7 (999) 123-45-67",
                    PostedDate = DateTime.UtcNow.AddDays(-5),
                    IsActive = true
                },
                new Service
                {
                    Id = 4,
                    Title = "Репетитор по английскому",
                    Category = "Образование",
                    Price = "2 000 ₽/час",
                    Description = "Подготовка к IELTS, TOEFL. Носитель языка.",
                    City = "Казань",
                    ProviderName = "Джон Смит",
                    ProviderType = "Физ. лицо",
                    Contacts = "john@email.com",
                    PostedDate = DateTime.UtcNow.AddDays(-2),
                    IsActive = true
                },
                new Service
                {
                    Id = 5,
                    Title = "Фотосъёмка мероприятий",
                    Category = "Творчество и дизайн",
                    Price = "15 000 ₽/час",
                    Description = "Профессиональная фотосъёмка свадеб, корпоративов.",
                    City = "Москва",
                    ProviderName = "Елена Фотограф",
                    ProviderType = "Физ. лицо",
                    Contacts = "@elena_photo",
                    PostedDate = DateTime.UtcNow.AddDays(-1),
                    IsActive = true
                }
            );

            // Заполнение справочников
            SeedDirectories(modelBuilder);
        }

        private void SeedDirectories(ModelBuilder modelBuilder)
        {
            // Группы городов
            var cityGroups = new[]
            {
                new { Id = 100, Name = "Москва и область", DirectoryType = "Cities", SortOrder = 1 },
                new { Id = 101, Name = "Санкт-Петербург", DirectoryType = "Cities", SortOrder = 2 },
                new { Id = 102, Name = "Центральный округ", DirectoryType = "Cities", SortOrder = 3 },
                new { Id = 103, Name = "Северо-Западный округ", DirectoryType = "Cities", SortOrder = 4 },
                new { Id = 104, Name = "Южный округ", DirectoryType = "Cities", SortOrder = 5 },
                new { Id = 105, Name = "Приволжский округ", DirectoryType = "Cities", SortOrder = 6 },
                new { Id = 106, Name = "Уральский округ", DirectoryType = "Cities", SortOrder = 7 },
                new { Id = 107, Name = "Сибирский округ", DirectoryType = "Cities", SortOrder = 8 },
                new { Id = 108, Name = "Дальневосточный округ", DirectoryType = "Cities", SortOrder = 9 },
                new { Id = 109, Name = "Другие страны", DirectoryType = "Cities", SortOrder = 10 }
            };

            foreach (var g in cityGroups)
            {
                modelBuilder.Entity<DirectoryGroup>().HasData(new DirectoryGroup
                {
                    Id = g.Id,
                    Name = g.Name,
                    DirectoryType = g.DirectoryType,
                    SortOrder = g.SortOrder
                });
            }

            // Города
            var cities = new (int Id, int GroupId, string Name, string? Description, int SortOrder)[]
            {
                (1001, 100, "Москва", "Столица России", 1),
                (1002, 100, "Химки", "Московская область", 2),
                (1003, 100, "Красногорск", "Московская область", 3),
                (1004, 100, "Подольск", "Московская область", 4),
                (1005, 100, "Мытищи", "Московская область", 5),
                (1006, 101, "Санкт-Петербург", "Северная столица", 1),
                (1007, 102, "Воронеж", "Центральный округ", 1),
                (1008, 102, "Ярославль", "Центральный округ", 2),
                (1009, 102, "Рязань", "Центральный округ", 3),
                (1010, 102, "Тула", "Центральный округ", 4),
                (1011, 102, "Калуга", "Центральный округ", 5),
                (1012, 103, "Калининград", "Северо-Западный округ", 1),
                (1013, 103, "Мурманск", "Северо-Западный округ", 2),
                (1014, 103, "Архангельск", "Северо-Западный округ", 3),
                (1015, 104, "Краснодар", "Южный округ", 1),
                (1016, 104, "Ростов-на-Дону", "Южный округ", 2),
                (1017, 104, "Сочи", "Южный округ", 3),
                (1018, 104, "Волгоград", "Южный округ", 4),
                (1019, 105, "Казань", "Приволжский округ", 1),
                (1020, 105, "Нижний Новгород", "Приволжский округ", 2),
                (1021, 105, "Самара", "Приволжский округ", 3),
                (1022, 105, "Уфа", "Приволжский округ", 4),
                (1023, 105, "Пермь", "Приволжский округ", 5),
                (1024, 106, "Екатеринбург", "Уральский округ", 1),
                (1025, 106, "Челябинск", "Уральский округ", 2),
                (1026, 106, "Тюмень", "Уральский округ", 3),
                (1027, 107, "Новосибирск", "Сибирский округ", 1),
                (1028, 107, "Красноярск", "Сибирский округ", 2),
                (1029, 107, "Омск", "Сибирский округ", 3),
                (1030, 107, "Иркутск", "Сибирский округ", 4),
                (1031, 108, "Владивосток", "Дальневосточный округ", 1),
                (1032, 108, "Хабаровск", "Дальневосточный округ", 2),
                (1033, 109, "Минск", "Беларусь", 1),
                (1034, 109, "Астана", "Казахстан", 2),
                (1035, 109, "Алматы", "Казахстан", 3),
                (1036, 109, "Ташкент", "Узбекистан", 4),
            };

            foreach (var c in cities)
            {
                modelBuilder.Entity<DirectoryItem>().HasData(new DirectoryItem
                {
                    Id = c.Id,
                    GroupId = c.GroupId,
                    Name = c.Name,
                    Description = c.Description,
                    SortOrder = c.SortOrder,
                    IsActive = true
                });
            }
        }
    }
}