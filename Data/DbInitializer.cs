using BackToYou.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackToYou.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created and up to date
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roleNames = { "Admin", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@campus.edu";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Campus Security Administrator",
                    StudentRollNumber = "STAFF-ADM-01",
                    Department = "Campus Security & Administration",
                    PhoneNumber = "9876543210",
                    CreatedAt = DateTime.UtcNow
                };

                var createAdminResult = await userManager.CreateAsync(adminUser, "Admin@123");
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Realistic Student Users
            var studentsData = new[]
            {
                new { Email = "student@campus.edu", Name = "Rahul Sharma", Roll = "CS2023045", Dept = "Computer Science & Engineering", Phone = "9876501234" },
                new { Email = "priya.patel@campus.edu", Name = "Priya Patel", Roll = "EC2023088", Dept = "Electronics & Communication", Phone = "9876501235" },
                new { Email = "ananya.reddy@campus.edu", Name = "Ananya Reddy", Roll = "ME2022019", Dept = "Mechanical Engineering", Phone = "9876501236" },
                new { Email = "vikram.singh@campus.edu", Name = "Vikram Singh", Roll = "CE2023062", Dept = "Civil Engineering", Phone = "9876501237" },
                new { Email = "sneha.deshmukh@campus.edu", Name = "Sneha Deshmukh", Roll = "BT2024012", Dept = "Biotechnology", Phone = "9876501238" }
            };

            var studentUsers = new List<ApplicationUser>();

            foreach (var s in studentsData)
            {
                var user = await userManager.FindByEmailAsync(s.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = s.Email,
                        Email = s.Email,
                        EmailConfirmed = true,
                        FullName = s.Name,
                        StudentRollNumber = s.Roll,
                        Department = s.Dept,
                        PhoneNumber = s.Phone,
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    };

                    var res = await userManager.CreateAsync(user, "Student@123");
                    if (res.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Student");
                    }
                }
                studentUsers.Add(user);
            }

            // 4. Seed Categories if empty
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Electronics & Gadgets", Description = "Laptops, phones, chargers, earphones, pendrives, calculators", IconClass = "bi-laptop" },
                    new Category { Name = "ID Cards & Documents", Description = "College ID cards, driver licenses, hall tickets, folders", IconClass = "bi-person-badge" },
                    new Category { Name = "Wallets, Bags & Purses", Description = "Backpacks, sling bags, pouches, coin wallets", IconClass = "bi-wallet2" },
                    new Category { Name = "Keys & Keychains", Description = "Room keys, bike/car keys, locker keys", IconClass = "bi-key" },
                    new Category { Name = "Books, Notes & Stationery", Description = "Textbooks, engineering notebooks, drawing sets", IconClass = "bi-book" },
                    new Category { Name = "Bottles & Lunch Boxes", Description = "Water flasks, tiffin containers, tumblers", IconClass = "bi-cup-straw" },
                    new Category { Name = "Clothing & Accessories", Description = "Jackets, watches, glasses, umbrella, caps", IconClass = "bi-smartwatch" },
                    new Category { Name = "Miscellaneous / Others", Description = "Sports gear, musical items, and other found items", IconClass = "bi-box-seam" }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            var allCategories = await context.Categories.ToListAsync();
            var elecCat = allCategories.FirstOrDefault(c => c.Name.Contains("Electronics"))?.Id ?? allCategories[0].Id;
            var idCat = allCategories.FirstOrDefault(c => c.Name.Contains("ID Cards"))?.Id ?? allCategories[0].Id;
            var bagCat = allCategories.FirstOrDefault(c => c.Name.Contains("Wallets"))?.Id ?? allCategories[0].Id;
            var keyCat = allCategories.FirstOrDefault(c => c.Name.Contains("Keys"))?.Id ?? allCategories[0].Id;
            var bookCat = allCategories.FirstOrDefault(c => c.Name.Contains("Books"))?.Id ?? allCategories[0].Id;
            var bottleCat = allCategories.FirstOrDefault(c => c.Name.Contains("Bottles"))?.Id ?? allCategories[0].Id;
            var clothCat = allCategories.FirstOrDefault(c => c.Name.Contains("Clothing"))?.Id ?? allCategories[0].Id;
            var miscCat = allCategories.FirstOrDefault(c => c.Name.Contains("Miscellaneous"))?.Id ?? allCategories[0].Id;

            // 5. Seed Realistic Dataset with Images (35 Items + Claims)
            // Re-seed if items lack images or dataset has fewer than 20 items with photos
            bool needsPhotos = !await context.Items.AnyAsync(i => i.ImagePath != null && i.ImagePath != "");
            if ((await context.Items.CountAsync() < 15 || needsPhotos) && studentUsers.Count >= 2 && adminUser != null)
            {
                // Clear any old items to provide the complete photo dataset
                context.Claims.RemoveRange(context.Claims);
                context.Items.RemoveRange(context.Items);
                await context.SaveChangesAsync();

                var u1 = studentUsers[0]; // Rahul Sharma
                var u2 = studentUsers[1]; // Priya Patel
                var u3 = studentUsers[2]; // Ananya Reddy
                var u4 = studentUsers[3]; // Vikram Singh
                var u5 = studentUsers[4]; // Sneha Deshmukh
                var adm = adminUser;      // Campus Admin

                var items = new List<Item>
                {
                    // ── Electronics & Gadgets ─────────────────────────
                    new Item
                    {
                        Title = "Found: Apple AirPods Pro (2nd Gen) in White Case",
                        Description = "Found near table 14 in the 2nd floor silent reading zone. Clean white case, fully charged.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Central Library 2nd Floor (Silent Reading Zone)",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u2.Id
                    },
                    new Item
                    {
                        Title = "Lost: Space Grey MacBook Air 13\" (M2)",
                        Description = "Laptop has a transparent matte hard case with GitHub and Linux octocat stickers on the lid. Left behind after guest lecture.",
                        Type = ItemType.Lost,
                        CategoryId = elecCat,
                        Location = "CS Department Seminar Hall 1",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u1.Id
                    },
                    new Item
                    {
                        Title = "Found: OnePlus 65W Warp Power Adapter & Red Cable",
                        Description = "Fast charger plugged into the wall outlet near the window corner booth. Kept at canteen billing desk.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Student Food Court & Canteen (Booth 4)",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1583863788434-e58a36330cf0?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: Samsung Galaxy Buds 2 (Lavender Case)",
                        Description = "Wireless earbuds in a compact lavender case with a small silicon cat paw pendant attached.",
                        Type = ItemType.Lost,
                        CategoryId = elecCat,
                        Location = "Indoor Sports Complex Gymnasium",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Found: Casio FX-991CW ClassWiz Scientific Calculator",
                        Description = "Grey and white Casio engineering calculator left on drafting bench 12 after mathematics exam.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Mechanical Workshop Building - Room B302",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1587145820266-a5951ee6f620?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Found: SanDisk Ultra Dual Drive 64GB USB-C",
                        Description = "Silver metallic swivel pen drive connected to Lab computer #22. Contains academic PDF slides.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Computer Center Lab 3",
                        EventDate = DateTime.Today.AddDays(-6),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        Status = ItemStatus.Claimed,
                        ImagePath = "https://images.unsplash.com/photo-1618410320928-25228d811631?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Found: Noise ColorFit Pulse Smart Fitness Watch (Black)",
                        Description = "Black silicon strap smartwatch with slight bezel scratch near top right corner. Charged to 45%.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Open Badminton Court Bench",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u5.Id
                    },
                    new Item
                    {
                        Title = "Lost: Boat Bassheads 242 Wired Sports Earphones",
                        Description = "Neon green and black wired earphones with ear-hooks inside a circular black zipper pouch.",
                        Type = ItemType.Lost,
                        CategoryId = elecCat,
                        Location = "Main Campus Gate Bus Stop",
                        EventDate = DateTime.Today.AddDays(-7),
                        CreatedAt = DateTime.UtcNow.AddDays(-7),
                        Status = ItemStatus.Closed,
                        ImagePath = "https://images.unsplash.com/photo-1546435770-a3e426bf472b?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u4.Id
                    },

                    // ── ID Cards & Documents ──────────────────────────
                    new Item
                    {
                        Title = "Found: Student ID Smartcard - ECE Department",
                        Description = "Official university RFID card. Student Name: Harish K, Roll No: EC2023088. Deposited with security office.",
                        Type = ItemType.Found,
                        CategoryId = idCat,
                        Location = "Main Security Checkpost Gate 1",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "https://images.unsplash.com/photo-1589330694653-dad6d3240a2b?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: Transparent Document Folder with Govt Driving License",
                        Description = "Clear A4 folder containing original Driving License, Aadhar photocopy, and 3 passport size photos.",
                        Type = ItemType.Lost,
                        CategoryId = idCat,
                        Location = "Library Xerox & Print Center",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1586281380349-632531db7ed4?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Found: Semester End Exam Hall Ticket & Clipboard",
                        Description = "Wooden exam clipboard with 6th sem university hall ticket for Civil Eng student clipped on.",
                        Type = ItemType.Found,
                        CategoryId = idCat,
                        Location = "Lecture Hall Complex Room 104",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Claimed,
                        ImagePath = "https://images.unsplash.com/photo-1584697964190-705b893a2095?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Lost: Semester Bus Pass (Route No. 14)",
                        Description = "Blue laminated college bus pass with hologram sticker. Route: Central Station to Campus.",
                        Type = ItemType.Lost,
                        CategoryId = idCat,
                        Location = "Student Parking Bay C",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1554224155-8d04cb21cd6c?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u1.Id
                    },
                    new Item
                    {
                        Title = "Found: SBI Global International Debit Card",
                        Description = "Blue chip debit card ending in digits 4821. Deposited at Admin finance counter.",
                        Type = ItemType.Found,
                        CategoryId = idCat,
                        Location = "Campus SBI ATM Booth",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "https://images.unsplash.com/photo-1563013544-824ae1b704d3?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = adm.Id
                    },

                    // ── Wallets, Bags & Purses ────────────────────────
                    new Item
                    {
                        Title = "Found: Wildcraft Ergonomic College Backpack (Navy Blue)",
                        Description = "Spacious backpack with rain cover tucked in bottom pocket. Contains a notebook and steel ruler.",
                        Type = ItemType.Found,
                        CategoryId = bagCat,
                        Location = "Central Library Reading Hall (Ground Floor)",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u5.Id
                    },
                    new Item
                    {
                        Title = "Lost: Brown Leather Tommy Hilfiger Bi-Fold Wallet",
                        Description = "Classic brown leather wallet containing college canteen tokens, cash, and metro smart card.",
                        Type = ItemType.Lost,
                        CategoryId = bagCat,
                        Location = "Nescafe Coffee Kiosk near Admin Lawn",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1627123424574-724758594e93?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u1.Id
                    },
                    new Item
                    {
                        Title = "Found: Skybags Pastel Pink Compact Sling Bag",
                        Description = "Zipper sling pouch with rose gold pullers. Contains hair accessories, lip balm, and locker key.",
                        Type = ItemType.Found,
                        CategoryId = bagCat,
                        Location = "University Auditorium Foyer",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Claimed,
                        ImagePath = "https://images.unsplash.com/photo-1590874103328-eac38a683ce7?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u2.Id
                    },
                    new Item
                    {
                        Title = "Lost: Black Nivia Sports Duffle Bag",
                        Description = "Medium sports duffle with football studs, shin guards, and college sports jersey #7.",
                        Type = ItemType.Lost,
                        CategoryId = bagCat,
                        Location = "University Football Ground Bleachers",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1577733966973-d680bffd2e80?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Found: Floral Canvas Tote Bag with Books",
                        Description = "Eco-friendly tote bag with sunflower pattern holding two engineering chemistry notebooks.",
                        Type = ItemType.Found,
                        CategoryId = bagCat,
                        Location = "Girls Hostel 2 Entrance Portico",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1597484661643-2f5fef640dd1?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u3.Id
                    },

                    // ── Keys & Keychains ──────────────────────────────
                    new Item
                    {
                        Title = "Found: Royal Enfield Classic Bike Key with Bullet Charm",
                        Description = "Single brass-accent bike ignition key with a miniature brass bullet pendant keychain.",
                        Type = ItemType.Found,
                        CategoryId = keyCat,
                        Location = "Two-Wheeler Parking Lot Stand #3",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1582139329536-e7284fece509?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: Set of 3 Hostel Room Keys on Blue Lanyard",
                        Description = "Three brass Godrej keys tagged with number 'B-304' on an official university blue neck ribbon.",
                        Type = ItemType.Lost,
                        CategoryId = keyCat,
                        Location = "Boys Hostel Block B Corridor",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1609172795856-11f26a117cb3?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u1.Id
                    },
                    new Item
                    {
                        Title = "Found: Yamaha FZ Bike Key with 'Ride or Die' Tag",
                        Description = "Black rubberized key with red woven fabric tag. Kept safely at Main Gate Guard Cabin.",
                        Type = ItemType.Found,
                        CategoryId = keyCat,
                        Location = "Campus Juice Corner Outdoor Seating",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Claimed,
                        ImagePath = "https://images.unsplash.com/photo-1584824486509-112e4181ff6b?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: Honda Activa Smart Key with Pikachu Charm",
                        Description = "Black electronic fob key with yellow silicone Pikachu keychain. Fell during evening walk.",
                        Type = ItemType.Lost,
                        CategoryId = keyCat,
                        Location = "Basketball Court Perimeter Walkway",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1616401784845-180882ba9ba8?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u2.Id
                    },

                    // ── Books, Notes & Stationery ─────────────────────
                    new Item
                    {
                        Title = "Found: 'Operating System Concepts' by Silberschatz (10th Ed)",
                        Description = "Blue hardcover textbook with slight highlighting in chapters 4 & 5. Name initial 'R.S.' written inside cover.",
                        Type = ItemType.Found,
                        CategoryId = bookCat,
                        Location = "Central Library Reading Hall A (Table 8)",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Lost: Thermodynamics Class Notebook & Engineering Drawing Sheets",
                        Description = "Spiral bound classmate notebook with handwritten notes and half-rolled imperial drawing sheets.",
                        Type = ItemType.Lost,
                        CategoryId = bookCat,
                        Location = "Mechanical Dept Drawing Hall 2",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1517842645767-c639042777db?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Found: Faber-Castell Complete Technical Drawing Geometry Kit",
                        Description = "Metallic tin box with compass, divider, set-squares, and French curves in pristine condition.",
                        Type = ItemType.Found,
                        CategoryId = bookCat,
                        Location = "Architecture Studio Building - 1st Floor",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1585776245991-cf89dd7fc73a?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u5.Id
                    },
                    new Item
                    {
                        Title = "Lost: Organic Chemistry Reaction Mechanism Notebook",
                        Description = "Dark green hardcover record book containing lab experiment summaries and signature stamps.",
                        Type = ItemType.Lost,
                        CategoryId = bookCat,
                        Location = "Biotechnology & Chemistry Lab Counter",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1532012164546-f432f2e3777f?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u5.Id
                    },

                    // ── Bottles & Lunch Boxes ─────────────────────────
                    new Item
                    {
                        Title = "Found: Milton 1000ml Stainless Steel Thermosteel Flask",
                        Description = "Silver insulated flask with black cap and nylon carrying strap. Keeps liquids hot/cold.",
                        Type = ItemType.Found,
                        CategoryId = bottleCat,
                        Location = "Open Air Amphitheatre Step 4",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1602143407151-7111542de6e8?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u2.Id
                    },
                    new Item
                    {
                        Title = "Lost: Tupperware 3-Tier Lunch Set in Insulated Grey Bag",
                        Description = "Grey thermal insulated bag containing three blue circular airtight containers and stainless fork.",
                        Type = ItemType.Lost,
                        CategoryId = bottleCat,
                        Location = "Campus Botanical Garden Gazebo",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Found: Decathlon Quechua 800ml Matte Black Sipper Bottle",
                        Description = "Matte black hiking sipper with measurement marks and lockable flip cap.",
                        Type = ItemType.Found,
                        CategoryId = bottleCat,
                        Location = "Cricket Pavilion Pavilion Seating",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Claimed,
                        ImagePath = "https://images.unsplash.com/photo-1523362628745-0c100150b504?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u4.Id
                    },

                    // ── Clothing & Accessories ────────────────────────
                    new Item
                    {
                        Title = "Found: Fastrack Polarized Aviator Sunglasses with Case",
                        Description = "Gunmetal finish frame sunglasses in black hard case with cleaning cloth.",
                        Type = ItemType.Found,
                        CategoryId = clothCat,
                        Location = "Campus Cafeteria Lawn Benches",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1511499767150-a48a237f0083?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u1.Id
                    },
                    new Item
                    {
                        Title = "Lost: Navy Blue Columbia Windcheater Jacket (Size M)",
                        Description = "Lightweight waterproof sports jacket with red zippers. Left on spectator stand during tournament.",
                        Type = ItemType.Lost,
                        CategoryId = clothCat,
                        Location = "Outdoor Basketball Court Pavilion",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1548883354-7622d03aca27?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Found: Titan Neo Quartz Analog Watch with Mesh Strap",
                        Description = "Silver dial analog watch with stainless steel mesh strap. Found near apparatus station 4.",
                        Type = ItemType.Found,
                        CategoryId = clothCat,
                        Location = "Physics Laboratory 1st Floor",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "https://images.unsplash.com/photo-1524805444758-089113d48a6d?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u5.Id
                    },
                    new Item
                    {
                        Title = "Lost: Ray-Ban Eyeglasses in Matte Black Wayfarer Frame",
                        Description = "Prescription reading glasses in black matte frame with blue anti-glare reflection.",
                        Type = ItemType.Lost,
                        CategoryId = clothCat,
                        Location = "Central Computing Facility (CCF)",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1591076482161-42ce6da69f67?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u2.Id
                    },

                    // ── Miscellaneous / Others ────────────────────────
                    new Item
                    {
                        Title = "Found: Yonex Nanoray 10F Badminton Racket with Cover",
                        Description = "Black/yellow tournament racket in original Yonex zipper sleeve left in court storage rack.",
                        Type = ItemType.Found,
                        CategoryId = miscCat,
                        Location = "Indoor Sports Complex Court 2",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: 3-Fold Compact Umbrella (Black & Red Plaid)",
                        Description = "Automatic push-button compact umbrella with checkered design and wrist cord.",
                        Type = ItemType.Lost,
                        CategoryId = miscCat,
                        Location = "Administrative Block Portico Steps",
                        EventDate = DateTime.Today.AddDays(-6),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        Status = ItemStatus.Closed,
                        ImagePath = "https://images.unsplash.com/photo-1517685352821-92cf88aee5a5?auto=format&fit=crop&w=800&q=80",
                        ReporterUserId = u3.Id
                    }
                };

                await context.Items.AddRangeAsync(items);
                await context.SaveChangesAsync();

                // 6. Seed Sample Claims with Proof Images and Remarks
                var airpodsItem = items.First(i => i.Title.Contains("AirPods"));
                var idCardItem = items.First(i => i.Title.Contains("Student ID Smartcard"));
                var usbDriveItem = items.First(i => i.Title.Contains("SanDisk Ultra"));
                var yamahaKeyItem = items.First(i => i.Title.Contains("Yamaha FZ"));
                var titanWatchItem = items.First(i => i.Title.Contains("Titan Neo Quartz"));
                var sbiCardItem = items.First(i => i.Title.Contains("SBI Global"));

                var claims = new List<Claim>
                {
                    new Claim
                    {
                        ItemId = airpodsItem.Id,
                        ClaimerUserId = u1.Id, // Rahul Sharma
                        DescriptionProof = "My AirPods case has an engraving 'PP 2024' on the front lid and the right AirPod has a small hairline scratch near the bottom sensor. Connected Bluetooth device name in my iPhone is 'Rahul's AirPods'.",
                        ProofImagePath = "https://images.unsplash.com/photo-1588423771073-b8903fbb85b5?auto=format&fit=crop&w=800&q=80",
                        SubmittedAt = DateTime.UtcNow.AddHours(-18),
                        Status = ClaimStatus.Pending,
                        AdminRemarks = null
                    },
                    new Claim
                    {
                        ItemId = idCardItem.Id,
                        ClaimerUserId = u3.Id, // Ananya Reddy
                        DescriptionProof = "It belongs to my batchmate Harish (Roll No EC2023088). I am submitting on his behalf and he can produce the original college fee receipt and aadhaar verification.",
                        ProofImagePath = "https://images.unsplash.com/photo-1589330694653-dad6d3240a2b?auto=format&fit=crop&w=800&q=80",
                        SubmittedAt = DateTime.UtcNow.AddHours(-10),
                        Status = ClaimStatus.Pending,
                        AdminRemarks = null
                    },
                    new Claim
                    {
                        ItemId = usbDriveItem.Id,
                        ClaimerUserId = u4.Id, // Vikram Singh
                        DescriptionProof = "The pen drive has two partitions: 'VIKRAM_STUDY' and 'PROJECTS'. Contains AutoCAD drawings titled 'Bridge_Deck_Analysis_v2.dwg'.",
                        ProofImagePath = "https://images.unsplash.com/photo-1618410320928-25228d811631?auto=format&fit=crop&w=800&q=80",
                        SubmittedAt = DateTime.UtcNow.AddDays(-5),
                        ReviewedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ClaimStatus.Approved,
                        AdminRemarks = "Verified folder names and student CAD files against Civil engineering enrollment. Handed over at Security Desk Room 102."
                    },
                    new Claim
                    {
                        ItemId = yamahaKeyItem.Id,
                        ClaimerUserId = u1.Id, // Rahul Sharma
                        DescriptionProof = "Yamaha key has ignition code stamped 'Y-942'. I have the vehicle RC document showing registration TS09-EA-4122 in my father's name.",
                        ProofImagePath = "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?auto=format&fit=crop&w=800&q=80",
                        SubmittedAt = DateTime.UtcNow.AddDays(-3),
                        ReviewedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ClaimStatus.Approved,
                        AdminRemarks = "Student brought bike to Main Gate security. Ignition turned successfully and RC documents matched. Claim approved & returned."
                    },
                    new Claim
                    {
                        ItemId = titanWatchItem.Id,
                        ClaimerUserId = u2.Id, // Priya Patel
                        DescriptionProof = "Watch was a birthday gift from my elder brother. The stainless steel caseback has a serial engraving ending in '892'.",
                        ProofImagePath = "https://images.unsplash.com/photo-1524805444758-089113d48a6d?auto=format&fit=crop&w=800&q=80",
                        SubmittedAt = DateTime.UtcNow.AddHours(-6),
                        Status = ClaimStatus.Pending,
                        AdminRemarks = null
                    },
                    new Claim
                    {
                        ItemId = sbiCardItem.Id,
                        ClaimerUserId = u5.Id, // Sneha Deshmukh
                        DescriptionProof = "The debit card has cardholder name printed in capital letters matching my SBI YONO profile screenshot.",
                        ProofImagePath = "https://images.unsplash.com/photo-1563013544-824ae1b704d3?auto=format&fit=crop&w=800&q=80",
                        SubmittedAt = DateTime.UtcNow.AddDays(-1),
                        ReviewedAt = DateTime.UtcNow.AddHours(-12),
                        Status = ClaimStatus.Rejected,
                        AdminRemarks = "Name on physical debit card (R. K. Verma) does not match student applicant profile. Please visit branch if this was an authorized family card."
                    }
                };

                await context.Claims.AddRangeAsync(claims);
                await context.SaveChangesAsync();
            }
        }
    }
}
