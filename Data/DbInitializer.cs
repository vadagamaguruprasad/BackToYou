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
            else
            {
                // Always guarantee admin user has the Admin role and active credentials
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                adminUser.EmailConfirmed = true;
                adminUser.LockoutEnd = null;
                adminUser.AccessFailedCount = 0;
                await userManager.UpdateAsync(adminUser);

                // Ensure password matches Admin@123
                if (!await userManager.CheckPasswordAsync(adminUser, "Admin@123"))
                {
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                    await userManager.ResetPasswordAsync(adminUser, resetToken, "Admin@123");
                }
            }

            // 3. Seed Realistic Student Users across Campus Departments
            var studentsData = new[]
            {
                new { Email = "student@campus.edu", Name = "Rahul Sharma", Roll = "CS2023045", Dept = "Computer Science & Engineering", Phone = "9876501234" },
                new { Email = "priya.patel@campus.edu", Name = "Priya Patel", Roll = "EC2023088", Dept = "Electronics & Communication", Phone = "9876501235" },
                new { Email = "ananya.reddy@campus.edu", Name = "Ananya Reddy", Roll = "ME2022019", Dept = "Mechanical Engineering", Phone = "9876501236" },
                new { Email = "vikram.singh@campus.edu", Name = "Vikram Singh", Roll = "CE2023062", Dept = "Civil Engineering", Phone = "9876501237" },
                new { Email = "sneha.deshmukh@campus.edu", Name = "Sneha Deshmukh", Roll = "BT2024012", Dept = "Biotechnology", Phone = "9876501238" },
                new { Email = "arjun.nair@campus.edu", Name = "Arjun Nair", Roll = "IT2023031", Dept = "Information Technology", Phone = "9876501239" },
                new { Email = "meera.iyer@campus.edu", Name = "Meera Iyer", Roll = "EE2023054", Dept = "Electrical & Electronics", Phone = "9876501240" },
                new { Email = "rohan.verma@campus.edu", Name = "Rohan Verma", Roll = "CH2022027", Dept = "Chemical Engineering", Phone = "9876501241" },
                new { Email = "tanvi.joshi@campus.edu", Name = "Tanvi Joshi", Roll = "AI2024089", Dept = "Data Science & AI", Phone = "9876501242" },
                new { Email = "karthik.sundaram@campus.edu", Name = "Karthik Sundaram", Roll = "AE2023015", Dept = "Aerospace Engineering", Phone = "9876501243" }
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

            // 5. Seed Comprehensive Realistic Dataset (95 Diverse Items + Claims)
            // Safety check: Only seed if demo items count < 80. Real user posts are NEVER deleted.
            var demoUserIds = studentUsers.Select(u => u.Id).Append(adminUser.Id).ToHashSet();
            var existingDemoItemCount = await context.Items.CountAsync(i => demoUserIds.Contains(i.ReporterUserId));

            if (existingDemoItemCount < 80 && studentUsers.Count >= 10 && adminUser != null)
            {
                // Safely remove only existing demo claims and demo items
                var demoClaims = await context.Claims
                    .Where(c => demoUserIds.Contains(c.ClaimerUserId) || (c.Item != null && demoUserIds.Contains(c.Item.ReporterUserId)))
                    .ToListAsync();
                context.Claims.RemoveRange(demoClaims);

                var oldDemoItems = await context.Items
                    .Where(i => demoUserIds.Contains(i.ReporterUserId))
                    .ToListAsync();
                context.Items.RemoveRange(oldDemoItems);
                await context.SaveChangesAsync();

                var u1 = studentUsers[0];  // Rahul Sharma (CS)
                var u2 = studentUsers[1];  // Priya Patel (ECE)
                var u3 = studentUsers[2];  // Ananya Reddy (Mech)
                var u4 = studentUsers[3];  // Vikram Singh (Civil)
                var u5 = studentUsers[4];  // Sneha Deshmukh (Biotech)
                var u6 = studentUsers[5];  // Arjun Nair (IT)
                var u7 = studentUsers[6];  // Meera Iyer (EEE)
                var u8 = studentUsers[7];  // Rohan Verma (Chem)
                var u9 = studentUsers[8];  // Tanvi Joshi (AI & DS)
                var u10 = studentUsers[9]; // Karthik Sundaram (Aerospace)
                var adm = adminUser;       // Campus Security Admin

                var items = new List<Item>
                {
                    // â”€â”€ 1. Electronics & Gadgets (15 Items) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    new Item
                    {
                        Title = "Found: Apple AirPods Pro (2nd Gen) in White MagSafe Case",
                        Description = "Found near table 14 in the 2nd floor silent reading zone. Clean white case with lanyard loop, fully charged.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Central Library 2nd Floor (Silent Reading Zone)",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "/images/items/airpods-pro.jpg",
                        ReporterUserId = u2.Id
                    },
                    new Item
                    {
                        Title = "Lost: Space Grey MacBook Air 13\" (M2) in Matte Shell",
                        Description = "Laptop has a transparent matte hard case with GitHub and Linux octocat stickers on the lid. Left behind after guest lecture.",
                        Type = ItemType.Lost,
                        CategoryId = elecCat,
                        Location = "CS Department Seminar Hall 1",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/macbook-air.jpg",
                        ReporterUserId = u1.Id
                    },
                    new Item
                    {
                        Title = "Found: OnePlus 65W Warp Power Adapter & Red Type-C Cable",
                        Description = "Fast charger plugged into the wall outlet near the window corner booth. Kept at canteen billing desk.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Student Food Court & Canteen (Booth 4)",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/oneplus-charger.jpg",
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
                        ImagePath = "/images/items/galaxy-buds.jpg",
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
                        ImagePath = "/images/items/casio-calculator.jpg",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Found: SanDisk Ultra Dual Drive 64GB USB-C Pen Drive",
                        Description = "Silver metallic swivel pen drive connected to Lab computer #22. Contains academic PDF slides.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Computer Center Lab 3",
                        EventDate = DateTime.Today.AddDays(-6),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        Status = ItemStatus.Claimed,
                        ImagePath = "/images/items/sandisk-pendrive.jpg",
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
                        ImagePath = "/images/items/noise-smartwatch.jpg",
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
                        ImagePath = "/images/items/boat-earphones.jpg",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Lost: iPhone 13 (128GB Starlight) in Clear Silicone Case",
                        Description = "Phone has a clear case with a small anime cat sticker between the case and phone body. Lock screen wallpaper shows a golden retriever.",
                        Type = ItemType.Lost,
                        CategoryId = elecCat,
                        Location = "Central Library East Lawn Benches",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/iphone-13.jpg",
                        ReporterUserId = u6.Id
                    },
                    new Item
                    {
                        Title = "Found: Mi 20000mAh Power Bank (Black Matte 18W)",
                        Description = "Heavy black dual-USB power bank found sitting on the bench next to the electrical power switchboard.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Electrical Machines Lab 2 (Workstation 8)",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/mi-powerbank.jpg",
                        ReporterUserId = u7.Id
                    },
                    new Item
                    {
                        Title = "Lost: Logitech Pebble M350 Wireless Bluetooth Mouse (Off-White)",
                        Description = "Slim pebble-shaped quiet click mouse. Battery compartment has one rechargeable AA cell.",
                        Type = ItemType.Lost,
                        CategoryId = elecCat,
                        Location = "Digital Learning Center 1st Floor",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/logitech-mouse.jpg",
                        ReporterUserId = u8.Id
                    },
                    new Item
                    {
                        Title = "Found: iPad 9th Gen (Space Grey) in Magnetic Navy Smart Folio",
                        Description = "Found on the podium shelf in Seminar Room 104. Screen is password locked with a white stylus in pen loop.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Biotechnology Seminar Room 104",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "/images/items/ipad-9thgen.jpg",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: Dell 65W Round-Tip AC Laptop Power Adapter",
                        Description = "Original Dell charger with thick power cord and rubber tie. Forgotten after practical design exam.",
                        Type = ItemType.Lost,
                        CategoryId = elecCat,
                        Location = "Civil CAD Studio Desk 15",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/dell-laptop-charger.jpg",
                        ReporterUserId = u9.Id
                    },
                    new Item
                    {
                        Title = "Found: Sony WH-CH520 Wireless Bluetooth Headphones (Black)",
                        Description = "Over-ear lightweight swivel headphones in clean condition with USB-C charging slot.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Audio Visual Media Lab (Booth 3)",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/sony-headphones.jpg",
                        ReporterUserId = u10.Id
                    },
                    new Item
                    {
                        Title = "Lost: Kindle Paperwhite (11th Gen 6.8\") in Sage Green Magnetic Cover",
                        Description = "E-reader in sage green PU leather case with slight corner wear. Home screen registered to 'Rohan V.'",
                        Type = ItemType.Lost,
                        CategoryId = elecCat,
                        Location = "Nescafe Lawn Garden Umbrella Table",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/kindle-paperwhite.jpg",
                        ReporterUserId = u8.Id
                    },

                    // â”€â”€ 2. ID Cards & Documents (12 Items) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                        ImagePath = "/images/items/student-id-smartcard.jpg",
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
                        ImagePath = "/images/items/driving-license-folder.jpg",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Found: Semester End Exam Hall Ticket & Wooden Clipboard",
                        Description = "Wooden exam clipboard with 6th sem university hall ticket for Civil Eng student clipped on.",
                        Type = ItemType.Found,
                        CategoryId = idCat,
                        Location = "Lecture Hall Complex Room 104",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Claimed,
                        ImagePath = "/images/items/exam-hallticket.jpg",
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
                        ImagePath = "/images/items/college-bus-pass.jpg",
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
                        ImagePath = "/images/items/sbi-debit-card.jpg",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: Final Year Project Synopsis Spiral Book (CS Dept)",
                        Description = "Navy blue plastic spiral booklet titled 'Distributed Lost & Found System Architecture'. Has guide's initial signature.",
                        Type = ItemType.Lost,
                        CategoryId = idCat,
                        Location = "Dean of Academics Office Waiting Area",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/project-synopsis-book.jpg",
                        ReporterUserId = u6.Id
                    },
                    new Item
                    {
                        Title = "Found: GATE 2026 Registration Receipt & State Bank Fee Challan",
                        Description = "Triplicate bank payment challan voucher folded inside yellow university envelope.",
                        Type = ItemType.Found,
                        CategoryId = idCat,
                        Location = "Main Auditorium Registration Counter",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/registration-challan.jpg",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: University Central Library Reader Barcode Card",
                        Description = "White laminated library borrower pass with student barcode sticker 'LIB-2023-441'.",
                        Type = ItemType.Lost,
                        CategoryId = idCat,
                        Location = "Library Reference Section 3rd Floor",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/library-membership-card.jpg",
                        ReporterUserId = u7.Id
                    },
                    new Item
                    {
                        Title = "Found: HDFC Bank Millennia Visa Contactless Credit Card",
                        Description = "Dark holographic card found dropped near cashier register. Card has been secured at campus bank branch.",
                        Type = ItemType.Found,
                        CategoryId = idCat,
                        Location = "Campus Juice Bar & Snack Corner",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "/images/items/hdfc-credit-card.jpg",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: Laminated 10th & 12th Board Marksheets Folder",
                        Description = "Hard plastic file folder holding photocopies of secondary certificates needed for campus recruitment verification.",
                        Type = ItemType.Lost,
                        CategoryId = idCat,
                        Location = "Training & Placement Cell Interview Room 3",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/marks-memo-folder.jpg",
                        ReporterUserId = u10.Id
                    },
                    new Item
                    {
                        Title = "Found: Hostel Resident Smart Identification Card & Mess Tokens",
                        Description = "Green border hostel resident pass with 15 breakfast mess coupons clipped to back.",
                        Type = ItemType.Found,
                        CategoryId = idCat,
                        Location = "Boys Hostel Dining Hall Mess Counter",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/hostel-resident-id.jpg",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Lost: Red Cross Blood Donation Donor Card & Blood Group Badge",
                        Description = "Red folding donor pass showing O-positive blood group with campus NSS camp stamp.",
                        Type = ItemType.Lost,
                        CategoryId = idCat,
                        Location = "Campus Health Center / First Aid Clinic",
                        EventDate = DateTime.Today.AddDays(-6),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        Status = ItemStatus.Closed,
                        ImagePath = "/images/items/blood-donation-card.jpg",
                        ReporterUserId = u5.Id
                    },

                    // â”€â”€ 3. Wallets, Bags & Purses (12 Items) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                        ImagePath = "/images/items/wildcraft-backpack.jpg",
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
                        ImagePath = "/images/items/tommy-leather-wallet.jpg",
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
                        ImagePath = "/images/items/skybags-pink-sling.jpg",
                        ReporterUserId = u2.Id
                    },
                    new Item
                    {
                        Title = "Lost: Black Nivia Sports Duffle Bag with Football Studs",
                        Description = "Medium sports duffle with football studs, shin guards, and college sports jersey #7.",
                        Type = ItemType.Lost,
                        CategoryId = bagCat,
                        Location = "University Football Ground Bleachers",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/sports-duffle-bag.jpg",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Found: Floral Canvas Tote Bag with Engineering Notebooks",
                        Description = "Eco-friendly tote bag with sunflower pattern holding two engineering chemistry notebooks.",
                        Type = ItemType.Found,
                        CategoryId = bagCat,
                        Location = "Girls Hostel 2 Entrance Portico",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/floral-canvas-tote.jpg",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Lost: American Tourister Grey Laptop Backpack (32L)",
                        Description = "Grey backpack with orange interior accents. Contains an engineering calculator and water bottle in side mesh.",
                        Type = ItemType.Lost,
                        CategoryId = bagCat,
                        Location = "Chemistry PG Block Staircase Landing",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/american-tourister-backpack.jpg",
                        ReporterUserId = u8.Id
                    },
                    new Item
                    {
                        Title = "Found: Small Black Leather Coin Purse with Locker Key",
                        Description = "Zipper pouch holding loose change, a brass padlock key, and two stamps.",
                        Type = ItemType.Found,
                        CategoryId = bagCat,
                        Location = "Girls Common Room Locker Bench",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/black-coin-purse.jpg",
                        ReporterUserId = u9.Id
                    },
                    new Item
                    {
                        Title = "Lost: Tan Brown Minimalist Leather Card Holder",
                        Description = "Slim 4-slot card holder with RFID blocking layer. Contains student ID and hostel access card.",
                        Type = ItemType.Lost,
                        CategoryId = bagCat,
                        Location = "Canara Bank Campus Extension Counter",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/tan-card-holder.jpg",
                        ReporterUserId = u7.Id
                    },
                    new Item
                    {
                        Title = "Found: Quechua 10L Mini Hiking Backpack (Blue/Grey)",
                        Description = "Compact daypack left on the steps of the adventure club room. Contains a folded rain poncho.",
                        Type = ItemType.Found,
                        CategoryId = bagCat,
                        Location = "Adventure & Nature Club Room",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/quechua-mini-backpack.jpg",
                        ReporterUserId = u10.Id
                    },
                    new Item
                    {
                        Title = "Lost: Fastrack Black Canvas Waist Pouch / Fanny Pack",
                        Description = "Black adjustable waist pack with headphone cord opening. Forgotten during evening cricket net session.",
                        Type = ItemType.Lost,
                        CategoryId = bagCat,
                        Location = "Cricket Practice Nets Pavilion",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/fastrack-waist-pouch.jpg",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Found: Black Document Messenger Bag with Lock",
                        Description = "Synthetic leather briefcase with shoulder strap. Contains staff meeting minutes and seminar agendas.",
                        Type = ItemType.Found,
                        CategoryId = bagCat,
                        Location = "Faculty Block 2 2nd Floor Corridor",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "/images/items/document-messenger-bag.jpg",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: Cartoon Pattern Canvas Pencil Pouch with Highlighters",
                        Description = "Yellow zipper pouch holding 4 pastel Stabilo Boss highlighters and a Staedtler mechanical pencil.",
                        Type = ItemType.Lost,
                        CategoryId = bagCat,
                        Location = "Mathematics Tutorial Room 208",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/pencil-pouch.jpg",
                        ReporterUserId = u2.Id
                    },

                    // â”€â”€ 4. Keys & Keychains (12 Items) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    new Item
                    {
                        Title = "Found: Royal Enfield Classic Bike Key with Brass Bullet Charm",
                        Description = "Single brass-accent bike ignition key with a miniature brass bullet pendant keychain.",
                        Type = ItemType.Found,
                        CategoryId = keyCat,
                        Location = "Two-Wheeler Parking Lot Stand #3",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/enfield-bike-key.jpg",
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
                        ImagePath = "/images/items/hostel-room-keys.jpg",
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
                        ImagePath = "/images/items/yamaha-bike-key.jpg",
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
                        ImagePath = "/images/items/activa-smart-key.jpg",
                        ReporterUserId = u2.Id
                    },
                    new Item
                    {
                        Title = "Found: Single Godrej Cupboard Key with Red Plastic Cap",
                        Description = "Number '34' stamped on key shank. Found inside open optics equipment drawer.",
                        Type = ItemType.Found,
                        CategoryId = keyCat,
                        Location = "Physics Optics Laboratory Drawer",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/godrej-cupboard-key.jpg",
                        ReporterUserId = u5.Id
                    },
                    new Item
                    {
                        Title = "Lost: KTM Duke 250 Ignition Key on Black Lanyard",
                        Description = "Orange accent KTM laser-cut key with reflective wrist lanyard strap. Dropped near vehicle security barrier.",
                        Type = ItemType.Lost,
                        CategoryId = keyCat,
                        Location = "Main Entrance Gate 2 Checkpoint",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/ktm-duke-key.jpg",
                        ReporterUserId = u6.Id
                    },
                    new Item
                    {
                        Title = "Found: Bicycle Cable Lock Key with Spiral Wrist Band",
                        Description = "Small silver key on stretchy blue coil wristband found hanging on cycle stand railing.",
                        Type = ItemType.Found,
                        CategoryId = keyCat,
                        Location = "Cycle Stand near Central Canteen",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/bicycle-lock-key.jpg",
                        ReporterUserId = u7.Id
                    },
                    new Item
                    {
                        Title = "Lost: Suzuki Access 125 Key with Marvel Iron Man Keychain",
                        Description = "Key with metallic red and gold Iron Man mask keychain. Fell out of pocket near girls parking.",
                        Type = ItemType.Lost,
                        CategoryId = keyCat,
                        Location = "Girls Hostel 1 Two-Wheeler Shed",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/access-scooter-key.jpg",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Found: Heavy Brass Padlock Key Stamped 'LINK-60'",
                        Description = "Sturdy brass key found on the gym threshold door step. Given to physical director.",
                        Type = ItemType.Found,
                        CategoryId = keyCat,
                        Location = "Indoor Gymnasium Equipment Room",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/padlock-key.jpg",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Lost: Hyundai i20 Smart Key Fob with Leather Sleeve",
                        Description = "Three-button flip remote in black leather protective case with chrome ring.",
                        Type = ItemType.Lost,
                        CategoryId = keyCat,
                        Location = "Faculty & Visitor Car Park (Slot B)",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/hyundai-car-key.jpg",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Found: Pair of Yale Keys with Glow-in-Dark Astronaut Charm",
                        Description = "Two cylinder keys on a white silicone astronaut keychain that glows light green in dark.",
                        Type = ItemType.Found,
                        CategoryId = keyCat,
                        Location = "Open Air Amphitheater Row 6",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "/images/items/astronaut-keychain.jpg",
                        ReporterUserId = u9.Id
                    },
                    new Item
                    {
                        Title = "Lost: Chemistry Research Cabinet Key Tagged 'CHEM-07'",
                        Description = "Small brass cabinet key with engraved acrylic rectangular tag 'CHEM-07'.",
                        Type = ItemType.Lost,
                        CategoryId = keyCat,
                        Location = "Organic Chemistry Research Annex",
                        EventDate = DateTime.Today.AddDays(-6),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        Status = ItemStatus.Closed,
                        ImagePath = "/images/items/cabinet-key-chem.jpg",
                        ReporterUserId = u8.Id
                    },

                    // â”€â”€ 5. Books, Notes & Stationery (12 Items) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                        ImagePath = "/images/items/os-concepts-textbook.jpg",
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
                        ImagePath = "/images/items/thermodynamics-notebook.jpg",
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
                        ImagePath = "/images/items/geometry-drawing-kit.jpg",
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
                        ImagePath = "/images/items/organic-chemistry-record.jpg",
                        ReporterUserId = u5.Id
                    },
                    new Item
                    {
                        Title = "Found: 'Database System Concepts' by Korth (7th Edition)",
                        Description = "Thick textbook with blue and orange cover. Name 'Arjun' written with pencil on top edge of pages.",
                        Type = ItemType.Found,
                        CategoryId = bookCat,
                        Location = "CS Departmental Library Shelf 4",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/dbms-korth-textbook.jpg",
                        ReporterUserId = u6.Id
                    },
                    new Item
                    {
                        Title = "Lost: Spiral Bound A4 Charcoal Sketchbook with Drawings",
                        Description = "Thick black cover sketchbook with charcoal architectural sketches and pencil portraits. Very sentimental value.",
                        Type = ItemType.Lost,
                        CategoryId = bookCat,
                        Location = "Fine Arts Club Studio Corner",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/charcoal-sketchbook.jpg",
                        ReporterUserId = u9.Id
                    },
                    new Item
                    {
                        Title = "Found: Parker Vector Stainless Steel Rollerball Pen",
                        Description = "Silver chrome finish rollerball pen with arrow pocket clip found under exam desk #14.",
                        Type = ItemType.Found,
                        CategoryId = bookCat,
                        Location = "Lecture Hall B1 Exam Desk 14",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/parker-pen.jpg",
                        ReporterUserId = u1.Id
                    },
                    new Item
                    {
                        Title = "Lost: Higher Engineering Mathematics by B.S. Grewal (44th Ed)",
                        Description = "Yellow-orange cover textbook with university bookstore invoice slipped inside page 200.",
                        Type = ItemType.Lost,
                        CategoryId = bookCat,
                        Location = "First Year Common Lecture Hall 12",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/maths-grewal-textbook.jpg",
                        ReporterUserId = u7.Id
                    },
                    new Item
                    {
                        Title = "Found: Clear Acrylic 30cm Technical Ruler & Set Square",
                        Description = "Professional bevelled edge drafting ruler inside plastic sheath left on drafting board.",
                        Type = ItemType.Found,
                        CategoryId = bookCat,
                        Location = "Civil CAD & Drafting Lab Desk 3",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/acrylic-ruler-set.jpg",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Lost: Machine Learning Lecture Slides Binder with Hand Notes",
                        Description = "White two-ring binder containing 120 printed slides on Neural Networks with purple highlighter remarks.",
                        Type = ItemType.Lost,
                        CategoryId = bookCat,
                        Location = "Artificial Intelligence Research Lab 4",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/ml-lecture-printouts.jpg",
                        ReporterUserId = u10.Id
                    },
                    new Item
                    {
                        Title = "Found: Medical Microbiology Illustrated Color Textbook",
                        Description = "Hardbound medical textbook left on the low reading table in pharmacy wing.",
                        Type = ItemType.Found,
                        CategoryId = bookCat,
                        Location = "Pharmacy & Allied Health Library Hall",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "/images/items/microbiology-textbook.jpg",
                        ReporterUserId = u5.Id
                    },
                    new Item
                    {
                        Title = "Lost: Black PU Leather Personal Journal / 2026 Daily Planner",
                        Description = "A5 diary with ribbon bookmark and gold foil year '2026' on front cover. Contains internship notes.",
                        Type = ItemType.Lost,
                        CategoryId = bookCat,
                        Location = "Campus Placement Training Lounge",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Closed,
                        ImagePath = "/images/items/pu-leather-diary.jpg",
                        ReporterUserId = u2.Id
                    },

                    // â”€â”€ 6. Bottles & Lunch Boxes (10 Items) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                        ImagePath = "/images/items/milton-steel-flask.jpg",
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
                        ImagePath = "/images/items/tupperware-lunch-set.jpg",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Found: Decathlon Quechua 800ml Matte Black Sipper Bottle",
                        Description = "Matte black hiking sipper with measurement marks and lockable flip cap.",
                        Type = ItemType.Found,
                        CategoryId = bottleCat,
                        Location = "Cricket Pavilion Seating",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Claimed,
                        ImagePath = "/images/items/decathlon-sipper.jpg",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Lost: Cello Puro Steel Insulated Water Bottle (Cyan Blue 900ml)",
                        Description = "Cyan blue metallic bottle with black handle cap. Has a small dent near the bottom rim.",
                        Type = ItemType.Lost,
                        CategoryId = bottleCat,
                        Location = "Tennis Court Perimeter Benches",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/cello-puro-cyan-bottle.jpg",
                        ReporterUserId = u1.Id
                    },
                    new Item
                    {
                        Title = "Found: Signoraware Compact Glass Tiffin with Red Insulated Bag",
                        Description = "Two borosilicate glass rectangular containers in red zipper carry pouch with cutlery.",
                        Type = ItemType.Found,
                        CategoryId = bottleCat,
                        Location = "ECE Department Staff & Student Lounge",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/signoraware-glass-tiffin.jpg",
                        ReporterUserId = u2.Id
                    },
                    new Item
                    {
                        Title = "Lost: Borosil Hydra Trek Flask (Army Green 750ml)",
                        Description = "Olive green insulated bottle with loop handle. Has a vinyl mountain sticker on the side.",
                        Type = ItemType.Lost,
                        CategoryId = bottleCat,
                        Location = "Library Lawn Tree Shade",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/borosil-army-green-flask.jpg",
                        ReporterUserId = u6.Id
                    },
                    new Item
                    {
                        Title = "Found: Protein Shaker Bottle with Wire Whisk Ball (Smoke Grey)",
                        Description = "BPA-free translucent shaker with red flip lid and stainless steel mixing sphere.",
                        Type = ItemType.Found,
                        CategoryId = bottleCat,
                        Location = "Indoor Gymnasium Free Weights Corner",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/protein-shaker-bottle.jpg",
                        ReporterUserId = u7.Id
                    },
                    new Item
                    {
                        Title = "Lost: Stainless Steel Coffee Travel Tumbler with Straw",
                        Description = "Double wall stainless steel insulated travel mug with splash-resistant lid.",
                        Type = ItemType.Lost,
                        CategoryId = bottleCat,
                        Location = "Management Studies Department Verandah",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/starbucks-tumbler.jpg",
                        ReporterUserId = u8.Id
                    },
                    new Item
                    {
                        Title = "Found: Milton Double-Wall Insulated Steel Flask (Silver 500ml)",
                        Description = "Slim metallic flask found behind backstage curtain during tech fest preparations.",
                        Type = ItemType.Found,
                        CategoryId = bottleCat,
                        Location = "Main Auditorium Backstage Dressing Room",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "/images/items/milton-doublewall-flask.jpg",
                        ReporterUserId = u9.Id
                    },
                    new Item
                    {
                        Title = "Lost: Yellow Sipper Bottle with Fruit Print Pattern",
                        Description = "Bright yellow sipper bottle with carrying strap. Forgotten near faculty guest quarters.",
                        Type = ItemType.Lost,
                        CategoryId = bottleCat,
                        Location = "Faculty Guest House Lawn Bench",
                        EventDate = DateTime.Today.AddDays(-6),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        Status = ItemStatus.Closed,
                        ImagePath = "/images/items/kids-pattern-sipper.jpg",
                        ReporterUserId = u10.Id
                    },

                    // â”€â”€ 7. Clothing & Accessories (12 Items) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                        ImagePath = "/images/items/fastrack-sunglasses.jpg",
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
                        ImagePath = "/images/items/columbia-windcheater.jpg",
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
                        ImagePath = "/images/items/titan-neo-watch.jpg",
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
                        ImagePath = "/images/items/rayban-eyeglasses.jpg",
                        ReporterUserId = u2.Id
                    },
                    new Item
                    {
                        Title = "Found: Fastrack Casual Watch with Denim Fabric Strap",
                        Description = "Youth edition casual analog watch with blue denim strap and silver buckle.",
                        Type = ItemType.Found,
                        CategoryId = clothCat,
                        Location = "Student Activity Center Indoor Games Room",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/fastrack-denim-watch.jpg",
                        ReporterUserId = u7.Id
                    },
                    new Item
                    {
                        Title = "Lost: Maroon University Varsity Fleece Hoodie (Size L)",
                        Description = "Comfortable maroon pullover hoodie with front kangaroo pocket and white collegiate letters.",
                        Type = ItemType.Lost,
                        CategoryId = clothCat,
                        Location = "Food Court Mezzanine Seating",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/harvard-maroon-hoodie.jpg",
                        ReporterUserId = u6.Id
                    },
                    new Item
                    {
                        Title = "Found: Grey Knitted Woolen Scarf & Beanie Cap",
                        Description = "Warm winter charcoal grey scarf left draped over wooden bench along running track.",
                        Type = ItemType.Found,
                        CategoryId = clothCat,
                        Location = "Early Morning Sports Track Pavilion",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/woolen-scarf-beanie.jpg",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Lost: Fossil Minimalist Slim Brown Leather Watch",
                        Description = "White face analog watch with tan leather band. Has a very small scratch on bezel at 6 o'clock position.",
                        Type = ItemType.Lost,
                        CategoryId = clothCat,
                        Location = "Placement Cell Interview Cabin 2",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/fossil-leather-watch.jpg",
                        ReporterUserId = u8.Id
                    },
                    new Item
                    {
                        Title = "Found: Casio Vintage Gold-Tone Digital Watch (A168)",
                        Description = "Classic gold stainless steel digital watch with Illuminator teal backlight. In full working order.",
                        Type = ItemType.Found,
                        CategoryId = clothCat,
                        Location = "Chemistry Department Water Dispenser",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/casio-vintage-gold.jpg",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: White Cotton Lab Apron / Doctor's Coat with Name Tag",
                        Description = "Full sleeve laboratory apron with embroidered name tag 'Dr. Sneha' and college medical crest.",
                        Type = ItemType.Lost,
                        CategoryId = clothCat,
                        Location = "Biotech Pathology Lab Station B",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/white-lab-apron.jpg",
                        ReporterUserId = u5.Id
                    },
                    new Item
                    {
                        Title = "Found: Black Baseball Cap with Embroidered 'NY' Logo",
                        Description = "Curved brim sports cap found on the sandy spectator mound by the volleyball court.",
                        Type = ItemType.Found,
                        CategoryId = clothCat,
                        Location = "Volleyball Sand Court Boundary",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/baseball-ny-cap.jpg",
                        ReporterUserId = u9.Id
                    },
                    new Item
                    {
                        Title = "Lost: Transparent Cat-Eye Reading Spectacles with Pink Case",
                        Description = "Clear acetate frame with pink tint arms in a magnetic snap triangular case.",
                        Type = ItemType.Lost,
                        CategoryId = clothCat,
                        Location = "Language Learning Lab Computer Desk 7",
                        EventDate = DateTime.Today.AddDays(-6),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        Status = ItemStatus.Closed,
                        ImagePath = "/images/items/cat-eye-spectacles.jpg",
                        ReporterUserId = u10.Id
                    },

                    // â”€â”€ 8. Miscellaneous / Others (10 Items) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                        ImagePath = "/images/items/yonex-badminton-racket.jpg",
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
                        ImagePath = "/images/items/compact-plaid-umbrella.jpg",
                        ReporterUserId = u3.Id
                    },
                    new Item
                    {
                        Title = "Found: Cosco Light Tennis Cricket Bat with White Rubber Grip",
                        Description = "Kashmir willow training bat found leaning against the fence behind batting nets.",
                        Type = ItemType.Found,
                        CategoryId = miscCat,
                        Location = "Cricket Ground Net 3",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/cosco-cricket-bat.jpg",
                        ReporterUserId = u4.Id
                    },
                    new Item
                    {
                        Title = "Lost: Yamaha Acoustic Guitar Capo & Dunlop Plectrum Case",
                        Description = "Black quick-change spring capo with a small tin containing four 0.88mm guitar picks.",
                        Type = ItemType.Lost,
                        CategoryId = miscCat,
                        Location = "Music Club Rehearsal Room (Room 12)",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/guitar-capo-picks.jpg",
                        ReporterUserId = u1.Id
                    },
                    new Item
                    {
                        Title = "Found: Studds Matte Black Bicycle Riding Helmet",
                        Description = "Aerodynamic cycling helmet with adjustable chin strap and rear red reflector.",
                        Type = ItemType.Found,
                        CategoryId = miscCat,
                        Location = "Cycle Stand near Gate 1",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/studds-bicycle-helmet.jpg",
                        ReporterUserId = adm.Id
                    },
                    new Item
                    {
                        Title = "Lost: Dark Blue Automatic Windproof Large Umbrella",
                        Description = "Golf size sturdy umbrella with curved foam grip handle and fiberglass ribs.",
                        Type = ItemType.Lost,
                        CategoryId = miscCat,
                        Location = "Guest House Front Porch Umbrella Stand",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/windproof-large-umbrella.jpg",
                        ReporterUserId = u7.Id
                    },
                    new Item
                    {
                        Title = "Found: 3M Littmann Classic III Medical Stethoscope (Black)",
                        Description = "Dual-sided chestpiece medical stethoscope in clean condition with personalized ID ring near bell.",
                        Type = ItemType.Found,
                        CategoryId = miscCat,
                        Location = "Campus Medical Inspection Room",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.UnderClaim,
                        ImagePath = "/images/items/medical-stethoscope.jpg",
                        ReporterUserId = u5.Id
                    },
                    new Item
                    {
                        Title = "Lost: Canon 58mm Camera Lens Front Cap",
                        Description = "Black pinch-clip lens cover with white Canon emblem. Left on tripod table during college fest shoot.",
                        Type = ItemType.Lost,
                        CategoryId = miscCat,
                        Location = "Photography Club Studio Desk",
                        EventDate = DateTime.Today.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/camera-lens-cap.jpg",
                        ReporterUserId = u8.Id
                    },
                    new Item
                    {
                        Title = "Found: Magnetic 3x3 Speed Cube with Stickerless Bright Tiles",
                        Description = "Smooth turning competition Rubik's cube found sitting on the indoor table tennis bench.",
                        Type = ItemType.Found,
                        CategoryId = miscCat,
                        Location = "Student Recreation Room Table 2",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/rubiks-cube.jpg",
                        ReporterUserId = u9.Id
                    },
                    new Item
                    {
                        Title = "Lost: Purple Eco-Friendly Yoga Mat in Black Mesh Bag",
                        Description = "6mm cushioned anti-skid workout mat with carrying sling strap. Forgotten after morning fitness session.",
                        Type = ItemType.Lost,
                        CategoryId = miscCat,
                        Location = "Open Yoga Lawn near Botanical Garden",
                        EventDate = DateTime.Today.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        Status = ItemStatus.Reported,
                        ImagePath = "/images/items/yoga-mat-bag.jpg",
                        ReporterUserId = u10.Id
                    }
                };

                await context.Items.AddRangeAsync(items);
                await context.SaveChangesAsync();

                // 6. Seed Realistic Claims with Proof Images and Admin Feedback
                var airpodsItem = items.First(i => i.Title.Contains("AirPods"));
                var idCardItem = items.First(i => i.Title.Contains("Student ID Smartcard"));
                var usbDriveItem = items.First(i => i.Title.Contains("SanDisk Ultra"));
                var yamahaKeyItem = items.First(i => i.Title.Contains("Yamaha FZ"));
                var titanWatchItem = items.First(i => i.Title.Contains("Titan Neo Quartz"));
                var sbiCardItem = items.First(i => i.Title.Contains("SBI Global"));
                var ipadItem = items.First(i => i.Title.Contains("iPad 9th Gen"));
                var stethoscopeItem = items.First(i => i.Title.Contains("Littmann Classic III"));

                var claims = new List<Claim>
                {
                    new Claim
                    {
                        ItemId = airpodsItem.Id,
                        ClaimerUserId = u1.Id, // Rahul Sharma
                        DescriptionProof = "My AirPods case has an engraving 'PP 2024' on the front lid and the right AirPod has a small hairline scratch near the bottom sensor. Connected Bluetooth device name in my iPhone is 'Rahul's AirPods'.",
                        ProofImagePath = "/images/items/airpods-pro.jpg",
                        SubmittedAt = DateTime.UtcNow.AddHours(-18),
                        Status = ClaimStatus.Pending,
                        AdminRemarks = null
                    },
                    new Claim
                    {
                        ItemId = idCardItem.Id,
                        ClaimerUserId = u3.Id, // Ananya Reddy
                        DescriptionProof = "It belongs to my batchmate Harish (Roll No EC2023088). I am submitting on his behalf and he can produce the original college fee receipt and aadhaar verification.",
                        ProofImagePath = "/images/items/student-id-smartcard.jpg",
                        SubmittedAt = DateTime.UtcNow.AddHours(-10),
                        Status = ClaimStatus.Pending,
                        AdminRemarks = null
                    },
                    new Claim
                    {
                        ItemId = usbDriveItem.Id,
                        ClaimerUserId = u4.Id, // Vikram Singh
                        DescriptionProof = "The pen drive has two partitions: 'VIKRAM_STUDY' and 'PROJECTS'. Contains AutoCAD drawings titled 'Bridge_Deck_Analysis_v2.dwg'.",
                        ProofImagePath = "/images/items/sandisk-pendrive.jpg",
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
                        ProofImagePath = "/images/items/yamaha-bike-key.jpg",
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
                        ProofImagePath = "/images/items/titan-neo-watch.jpg",
                        SubmittedAt = DateTime.UtcNow.AddHours(-6),
                        Status = ClaimStatus.Pending,
                        AdminRemarks = null
                    },
                    new Claim
                    {
                        ItemId = sbiCardItem.Id,
                        ClaimerUserId = u5.Id, // Sneha Deshmukh
                        DescriptionProof = "The debit card has cardholder name printed in capital letters matching my SBI YONO profile screenshot.",
                        ProofImagePath = "/images/items/sbi-debit-card.jpg",
                        SubmittedAt = DateTime.UtcNow.AddDays(-1),
                        ReviewedAt = DateTime.UtcNow.AddHours(-12),
                        Status = ClaimStatus.Rejected,
                        AdminRemarks = "Name on physical debit card (R. K. Verma) does not match student applicant profile. Please visit branch if this was an authorized family card."
                    },
                    new Claim
                    {
                        ItemId = ipadItem.Id,
                        ClaimerUserId = u6.Id, // Arjun Nair
                        DescriptionProof = "iPad passcode is 6 digits ending in 88. Apple ID matches arjun.nair@campus.edu. Lock screen notification shows unread Canvas assignments.",
                        ProofImagePath = "/images/items/ipad-9thgen.jpg",
                        SubmittedAt = DateTime.UtcNow.AddHours(-8),
                        Status = ClaimStatus.Pending,
                        AdminRemarks = null
                    },
                    new Claim
                    {
                        ItemId = stethoscopeItem.Id,
                        ClaimerUserId = u5.Id, // Sneha Deshmukh
                        DescriptionProof = "Engraved with initials 'S.D.' on the binaural tube clamp. Belonging to final year clinical biochemistry lab kit.",
                        ProofImagePath = "/images/items/medical-stethoscope.jpg",
                        SubmittedAt = DateTime.UtcNow.AddHours(-4),
                        Status = ClaimStatus.Pending,
                        AdminRemarks = null
                    }
                };

                await context.Claims.AddRangeAsync(claims);
                await context.SaveChangesAsync();
            }

            // 7. Ensure AntiRaggingComplaints table exists (safe for both existing SQLite & PostgreSQL)
            try
            {
                if (context.Database.IsSqlite())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS ""AntiRaggingComplaints"" (
                            ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                            ""ReferenceNumber"" TEXT NOT NULL,
                            ""StudentName"" TEXT NULL,
                            ""RollNumber"" TEXT NULL,
                            ""RaggingType"" INTEGER NOT NULL,
                            ""IncidentDate"" TEXT NOT NULL,
                            ""Location"" TEXT NOT NULL,
                            ""Description"" TEXT NOT NULL,
                            ""AccusedDetails"" TEXT NULL,
                            ""Witnesses"" TEXT NULL,
                            ""ContactEmail"" TEXT NULL,
                            ""ContactPhone"" TEXT NULL,
                            ""IsConfidential"" INTEGER NOT NULL,
                            ""Status"" INTEGER NOT NULL,
                            ""ActionTaken"" TEXT NULL,
                            ""CreatedAt"" TEXT NOT NULL,
                            ""UpdatedAt"" TEXT NULL,
                            ""ReviewedBy"" TEXT NULL
                        );
                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AntiRaggingComplaints_ReferenceNumber"" ON ""AntiRaggingComplaints"" (""ReferenceNumber"");
                    ");
                }
                else
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS ""AntiRaggingComplaints"" (
                            ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                            ""ReferenceNumber"" character varying(30) NOT NULL,
                            ""StudentName"" character varying(100) NULL,
                            ""RollNumber"" character varying(50) NULL,
                            ""RaggingType"" integer NOT NULL,
                            ""IncidentDate"" timestamp with time zone NOT NULL,
                            ""Location"" character varying(150) NOT NULL,
                            ""Description"" character varying(3000) NOT NULL,
                            ""AccusedDetails"" character varying(500) NULL,
                            ""Witnesses"" character varying(500) NULL,
                            ""ContactEmail"" character varying(100) NULL,
                            ""ContactPhone"" character varying(20) NULL,
                            ""IsConfidential"" boolean NOT NULL,
                            ""Status"" integer NOT NULL,
                            ""ActionTaken"" character varying(1500) NULL,
                            ""CreatedAt"" timestamp with time zone NOT NULL,
                            ""UpdatedAt"" timestamp with time zone NULL,
                            ""ReviewedBy"" character varying(100) NULL
                        );
                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AntiRaggingComplaints_ReferenceNumber"" ON ""AntiRaggingComplaints"" (""ReferenceNumber"");
                    ");
                }
            }
            catch
            {
                // Table already exists or managed by EF Core
            }

            // 8. Seed Sample Anti-Ragging Complaints if empty
            if (!await context.AntiRaggingComplaints.AnyAsync())
            {
                var sampleComplaints = new List<AntiRaggingComplaint>
                {
                    new AntiRaggingComplaint
                    {
                        ReferenceNumber = "ARC-2026-81924",
                        StudentName = "Anonymous",
                        RollNumber = "CS2025014",
                        RaggingType = RaggingType.Verbal,
                        IncidentDate = DateTime.Today.AddDays(-3),
                        Location = "Central Student Canteen Annex",
                        Description = "Group of 3 seniors repeatedly blocked seating tables, demanded that first-year students address them with derogatory prefixes, and used abusive language when ignored.",
                        AccusedDetails = "3 students wearing 3rd year mechanical department badges, one named 'Vikram' or similar.",
                        Witnesses = "Canteen juice counter staff and several 1st year students.",
                        ContactEmail = "confidential.student@campus.edu",
                        IsConfidential = true,
                        Status = ComplaintStatus.ActionInProgress,
                        ActionTaken = "Anti-Ragging Flying Squad conducted surprise inspection at Canteen. Disciplinary committee summoned senior students; written undertakings obtained.",
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                        ReviewedBy = "Campus Security Administrator"
                    },
                    new AntiRaggingComplaint
                    {
                        ReferenceNumber = "ARC-2026-64015",
                        StudentName = "Rahul M.",
                        RollNumber = "ME2025088",
                        RaggingType = RaggingType.Emotional,
                        IncidentDate = DateTime.Today.AddDays(-6),
                        Location = "Boys Hostel Block B 2nd Floor Corridor",
                        Description = "Forced late-night assembly in wing common room past midnight, requiring juniors to perform humiliating acts and complete seniors' academic record drawings.",
                        AccusedDetails = "Hostel B Room 204 & 208 residents.",
                        Witnesses = "Hostel Floor Warden and security guard on night patrol.",
                        ContactEmail = "rahul.m@campus.edu",
                        ContactPhone = "9876504321",
                        IsConfidential = true,
                        Status = ComplaintStatus.Resolved,
                        ActionTaken = "CCTV footage reviewed by Chief Warden. Formal warning letters issued to accused students' parents. Wing monitors reassigned.",
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2),
                        ReviewedBy = "Campus Security Administrator"
                    },
                    new AntiRaggingComplaint
                    {
                        ReferenceNumber = "ARC-2026-93201",
                        StudentName = "Anonymous",
                        RollNumber = "EC2025042",
                        RaggingType = RaggingType.Cyberbullying,
                        IncidentDate = DateTime.Today.AddDays(-1),
                        Location = "Unofficial College Telegram / WhatsApp Group",
                        Description = "Circulation of unauthorized derogatory memes and threatening direct messages targeting newly joined department batch students.",
                        AccusedDetails = "Admin of 'Campus Freshers 2026' unofficial group.",
                        Witnesses = "Screenshots attached with timestamp verification.",
                        ContactEmail = null,
                        IsConfidential = true,
                        Status = ComplaintStatus.Submitted,
                        ActionTaken = null,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    }
                };

                await context.AntiRaggingComplaints.AddRangeAsync(sampleComplaints);
                await context.SaveChangesAsync();
            }
        }
    }
}