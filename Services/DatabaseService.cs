using CatHotel.Models;
using Microcharts;
using SkiaSharp;
using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CatHotel.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _db;
        private readonly string _dbPath;

        public DatabaseService(string dbPath)
        {
            _dbPath = dbPath;
            _db = new SQLiteAsyncConnection(dbPath);

        }

        public SQLiteAsyncConnection Db => _db;
        private bool _initialized;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public async Task InitializeAsync()
        {
            if (_initialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;

                await _db.CreateTableAsync<Room>();
                await _db.CreateTableAsync<Customer>();
                await _db.CreateTableAsync<Cat>();
                await _db.CreateTableAsync<Discount>();
                await _db.CreateTableAsync<Booking>();
                await _db.CreateTableAsync<BookingItem>();
                await _db.CreateTableAsync<BookingCat>();
                await _db.CreateTableAsync<ShopItem>();
                await _db.CreateTableAsync<Sale>();
                await _db.CreateTableAsync<OutcomeRecord>();

                await EnsureColumnExistsAsync("Cats", "CustomerId", "INTEGER", "0");
                await EnsureColumnExistsAsync("Bookings", "TotalPrice", "REAL", "0");
                await EnsureColumnExistsAsync("BookingItems", "UnitPrice", "REAL", "0");
                await EnsureColumnExistsAsync("BookingItems", "Quantity", "INTEGER", "1");
                await EnsureColumnExistsAsync("Sales", "DiscountId", "INTEGER", null);

                // ถ้าจะเทสให้เอาcommentออก
                //await SeedTestDataIfEmptyAsync();

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Deletes the database file and resets the initialization state.
        /// Call this when you need to reset the database (e.g., due to schema conflicts).
        /// After calling this, you can call InitializeAsync() again to create a fresh database.
        /// </summary>
        public async Task DeleteDatabaseAsync()
        {
            await _initLock.WaitAsync();
            try
            {
                // Close the connection
                await _db.CloseAsync();

                // Delete the database file if it exists
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                    System.Diagnostics.Debug.WriteLine($"[DB] Database file deleted: {_dbPath}");
                }

                // Reset the initialization state so InitializeAsync() will recreate the database
                _initialized = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB] Error deleting database: {ex.Message}");
                throw;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private class PragmaTableInfo { public string name { get; set; } }

        private async Task EnsureColumnExistsAsync(string table, string column, string type, string defaultValue)
        {
            var cols = await _db.QueryAsync<PragmaTableInfo>($"PRAGMA table_info(\"{table}\");");
            if (cols.Any(c => string.Equals(c.name, column, StringComparison.OrdinalIgnoreCase)))
                return;

            await _db.ExecuteAsync($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {type};");

            if (defaultValue != null)
            {
                await _db.ExecuteAsync($"UPDATE \"{table}\" SET \"{column}\" = {defaultValue} WHERE \"{column}\" IS NULL;");
            }
        }

        public async Task RecalculateBookingTotalPriceAsync(int bookingId)
        {
            var booking = await _db.Table<Booking>()
                .Where(x => x.Id == bookingId)
                .FirstOrDefaultAsync();

            if (booking == null) return;

            var items = await _db.Table<BookingItem>()
                .Where(x => x.BookingId == bookingId)
                .ToListAsync();

            var room = await _db.Table<Room>()
                .Where(x => x.Id == booking.RoomId)
                .FirstOrDefaultAsync();

            // Calculate room charge for the booking period
            var nights = Math.Max(1, (booking.EndDate.Date - booking.StartDate.Date).Days);
            var roomCharge = (room?.BasePrice ?? 0) * nights;

            // Calculate shop items total
            var shopTotal = items.Sum(x => x.Quantity * x.UnitPrice);

            // Total = room charge + shop items
            var total = roomCharge + shopTotal;

            await _db.ExecuteAsync("UPDATE Bookings SET TotalPrice = ? WHERE Id = ?", total, bookingId);
        }

        public async Task<List<(DateTime Month, double Income, double Expense)>> GetMonthlySalesByYearAsync(int year)
        {
            var start = new DateTime(year, 1, 1);
            var end = start.AddYears(1);

            // ✅ REFACTORED: Fetch all SALES (completed transactions) for the year
            var sales = await _db.Table<Sale>()
                .Where(s => s.CompletedAt >= start && s.CompletedAt < end)
                .ToListAsync();

            var outcomes = await _db.Table<OutcomeRecord>()
                .Where(o => o.CreatedAt >= start && o.CreatedAt < end)
                .ToListAsync();

            var result = new List<(DateTime Month, double Income, double Expense)>();
            for (int i = 1; i <= 12; i++)
            {
                var m = new DateTime(year, i, 1);

                // ✅ Calculate income from SALES table (TotalRevenue)
                var income = sales
                    .Where(s => s.CompletedAt.Year == m.Year && s.CompletedAt.Month == m.Month)
                    .Sum(s => s.TotalRevenue);

                // Calculate expenses from OUTCOMES table (remains unchanged)
                double expense = outcomes
                    .Where(o => o.CreatedAt.Year == m.Year && o.CreatedAt.Month == m.Month)
                    .Sum(o => o.Amount);

                result.Add((m, income, expense));
            }
            return result;
        }

        public async Task<(int Large, int Medium, int Small)> GetRoomUsageCountByTypeAsync(int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // Fetch all sales for the month
            var sales = await _db.Table<Sale>()
                .Where(s => s.CompletedAt >= monthStart && s.CompletedAt < monthEnd)
                .ToListAsync();

            if (sales.Count == 0) return (0, 0, 0);

            var roomIdList = sales.Select(s => s.RoomId).Distinct().ToList();

            // ถ้า Contains แปลเป็น SQL ไม่ได้ในบางเครื่อง ให้เปลี่ยนเป็นดึง Room ทั้งหมดแล้วกรองใน memory
            var rooms = await _db.Table<Room>()
                .Where(r => roomIdList.Contains(r.Id))
                .ToListAsync();

            var roomTypeById = rooms.ToDictionary(r => r.Id, r => r.RoomType);

            int large = 0, medium = 0, small = 0;

            foreach (var s in sales)
            {
                if (!roomTypeById.TryGetValue(s.RoomId, out var roomType)) continue;

                switch (roomType)
                {
                    case RoomTypes.Large:
                        large++;
                        break;
                    case RoomTypes.Medium:
                        medium++;
                        break;
                    case RoomTypes.Small:
                        small++;
                        break;
                }
            }

            return (large, medium, small);
        }

        public async Task<Dictionary<string, float>> GetItemTypeQuantityByMonthAsync(int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // Fetch all sales for the month
            var sales = await _db.Table<Sale>()
                .Where(s => s.CompletedAt >= monthStart && s.CompletedAt < monthEnd)
                .ToListAsync();

            var bookingIds = sales.Select(s => s.BookingId).Distinct().ToList();
            if (bookingIds.Count == 0)
                return new Dictionary<string, float>();

            var bookingItems = await _db.Table<BookingItem>()
                .Where(bi => bookingIds.Contains(bi.BookingId))
                .ToListAsync();

            if (bookingItems.Count == 0)
                return new Dictionary<string, float>();

            var itemIds = bookingItems.Select(bi => bi.ItemId).Distinct().ToList();

            var items = await _db.Table<ShopItem>()
                .Where(i => itemIds.Contains(i.Id))
                .ToListAsync();

            var itemTypeById = items.ToDictionary(i => i.Id, i => i.ItemType.ToString());

            var result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

            foreach (var bi in bookingItems)
            {
                if (!itemTypeById.TryGetValue(bi.ItemId, out var itemType))
                    continue;

                if (string.IsNullOrWhiteSpace(itemType))
                    itemType = "Unknown";

                if (!result.ContainsKey(itemType))
                    result[itemType] = 0;

                result[itemType] += bi.Quantity;
            }

            return result;
        }


        // ---- OutcomeRecord CRUD ----

        public async Task<int> AddOutcomeRecordAsync(OutcomeRecord record)
        {
            await InitializeAsync();
            record.CreatedAt = DateTime.Now;
            return await _db.InsertAsync(record);
        }

        public async Task<List<OutcomeRecord>> GetAllOutcomeRecordsAsync()
        {
            await InitializeAsync();
            return await _db.Table<OutcomeRecord>()
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<OutcomeRecord>> GetOutcomeRecordsByFilterAsync(int? year, int? month, int? day)
        {
            await InitializeAsync();
            var all = await _db.Table<OutcomeRecord>().ToListAsync();

            var filtered = all.AsEnumerable();
            if (year.HasValue)  filtered = filtered.Where(r => r.CreatedAt.Year  == year.Value);
            if (month.HasValue) filtered = filtered.Where(r => r.CreatedAt.Month == month.Value);
            if (day.HasValue)   filtered = filtered.Where(r => r.CreatedAt.Day   == day.Value);

            return filtered.OrderByDescending(r => r.CreatedAt).ToList();
        }

        public async Task<int> DeleteOutcomeRecordAsync(int id)
        {
            await InitializeAsync();
            return await _db.DeleteAsync<OutcomeRecord>(id);
        }

        // ---- End OutcomeRecord CRUD ----

        //เทสข้อมูลอย่าลืมเอาออก
        private async Task SeedTestDataIfEmptyAsync()
        {
            await _db.DeleteAllAsync<BookingItem>();
            await _db.DeleteAllAsync<Booking>();
            await _db.DeleteAllAsync<ShopItem>();
            await _db.DeleteAllAsync<Customer>();
            await _db.DeleteAllAsync<Room>();
            // กัน seed ซ้ำ

            int year = DateTime.Now.Year;

            // 1) Room
            var roomLarge = new Room
            {
                Name = "Room L1",
                Status = RoomStatus.Available,
                RoomType = RoomTypes.Large,
                MaxOccupants = 3,
                BasePrice = 1200,
                EndDate = new DateTime(year, 12, 31),
                ImgUrl = "seed://room-large"
            };
            var roomMedium = new Room
            {
                Name = "Room M1",
                Status = RoomStatus.Available,
                RoomType = RoomTypes.Medium,
                MaxOccupants = 2,
                BasePrice = 900,
                EndDate = new DateTime(year, 12, 31),
                ImgUrl = "seed://room-medium"
            };
            var roomSmall = new Room
            {
                Name = "Room S1",
                Status = RoomStatus.Available,
                RoomType = RoomTypes.Small,
                MaxOccupants = 1,
                BasePrice = 600,
                EndDate = new DateTime(year, 12, 31),
                ImgUrl = "seed://room-small"
            };

            await _db.InsertAsync(roomLarge);
            await _db.InsertAsync(roomMedium);
            await _db.InsertAsync(roomSmall);

            // 2) Customer
            var customer = new Customer
            {
                Name = "Seed Customer",
                TelephoneNum = "0999999999",
                Email = "seed@example.com",
                ImgUrl = "seed://customer"
            };
            await _db.InsertAsync(customer);

            // 3) ShopItems (เพิ่ม/ลดได้ตาม ItemType ที่มีจริง)
            var food = new ShopItem
            {
                Name = "Cat Food",
                Description = "Seed food item",
                ItemPrice = 50,
                ItemType = ItemType.Food,
                ImgUrl = "seed://food"
            };
            var toy = new ShopItem
            {
                Name = "Cat Toy",
                Description = "Seed toy item",
                ItemPrice = 30,
                ItemType = ItemType.Toy,
                ImgUrl = "seed://toy"
            };
            var necessity = new ShopItem
            {
                Name = "Cat Necessity",
                Description = "Seed necessity item",
                ItemPrice = 20,
                ItemType = ItemType.Necessity,
                ImgUrl = "seed://necessity"
            };

            await _db.InsertAsync(food);
            await _db.InsertAsync(toy);
            await _db.InsertAsync(necessity);
            var plan = new (int Food, int Toy, int Necessity)[]
                {
                    (12, 2,  5),  // Jan
                    (6,  9,  2),  // Feb
                    (3,  4,  14), // Mar
                    (10, 5,  3),  // Apr
                    (4,  12, 6),  // May
                    (8,  3,  11), // Jun
                    (15, 1,  4),  // Jul
                    (5,  14, 2),  // Aug
                    (2,  6,  16), // Sep
                    (11, 7,  3),  // Oct
                    (3,  15, 5),  // Nov
                    (7,  2,  18), // Dec
                };

            // 4) Seed bookings ครบ 12 เดือน + booking items
            // ทำให้เห็นความต่างชัด: แต่ละเดือน quantity จะ "สลับเด่น"
            for (int month = 1; month <= 12; month++)
            {
                var start = new DateTime(year, month, 10);
                var end = new DateTime(year, month, 12); // ให้ EndDate อยู่ในเดือนนั้นแน่นอน

                var (foodQty, toyQty, necQty) = plan[month - 1];
                double totalPrice =
                       (foodQty * food.ItemPrice) +
                       (toyQty * toy.ItemPrice) +
                       (necQty * necessity.ItemPrice);

                // ทำให้กราฟ 3 ไม่เป็น 1 ตลอด:
                // เดือนหนึ่งสร้างหลาย booking และ "เน้น" ห้องคนละประเภทสลับกัน
                int largeBookings = (month % 3 == 1) ? 3 : 1;
                int mediumBookings = (month % 3 == 2) ? 3 : 1;
                int smallBookings = (month % 3 == 0) ? 3 : 1;

                async Task InsertBookingWithItemsAsync(int roomId, int count)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var booking = new Booking
                        {
                            RoomId = roomId,
                            CustomerId = customer.Id,
                            StartDate = start,
                            EndDate = end,
                            DiscountId = null,
                            TotalPrice = totalPrice
                        };
                        await _db.InsertAsync(booking);

                        // จะให้แต่ละ booking ในเดือนเดียวกันเหมือนกันก็ได้ (ง่ายสุด)
                        await _db.InsertAsync(new BookingItem(booking.Id, food.Id, quantity: foodQty)
                        {
                            UnitPrice = food.ItemPrice
                        });

                        await _db.InsertAsync(new BookingItem(booking.Id, toy.Id, quantity: toyQty)
                        {
                            UnitPrice = toy.ItemPrice
                        });

                        await _db.InsertAsync(new BookingItem(booking.Id, necessity.Id, quantity: necQty)
                        {
                            UnitPrice = necessity.ItemPrice
                        });
                    }
                }

                await InsertBookingWithItemsAsync(roomLarge.Id, largeBookings);
                await InsertBookingWithItemsAsync(roomMedium.Id, mediumBookings);
                await InsertBookingWithItemsAsync(roomSmall.Id, smallBookings);
            }

            // Debug (optional)
            var bookingCount = await _db.Table<Booking>().CountAsync();
            System.Diagnostics.Debug.WriteLine($"[SEED] Booking count = {bookingCount}");

            var months = (await _db.Table<Booking>().ToListAsync())
                .Select(b => b.EndDate.Month)
                .OrderBy(m => m);

            System.Diagnostics.Debug.WriteLine("[SEED] EndDate months: " + string.Join(", ", months));
        }
        //เทสข้อมูลอย่าลืมเอาออก

        // ---- Separate Seeding Method for Mock Data ----
        public async Task SeedMockDataAsync()
        {
            await InitializeAsync();

            // Clear existing data (this was called from App.xaml.cs anyways, uncomment if needed)
            //await _db.DeleteAllAsync<BookingCat>();
            //await _db.DeleteAllAsync<BookingItem>();
            //await _db.DeleteAllAsync<Booking>();
            //await _db.DeleteAllAsync<Cat>();
            //await _db.DeleteAllAsync<Customer>();
            //await _db.DeleteAllAsync<Room>();
            //await _db.DeleteAllAsync<Sale>();
            //await _db.DeleteAllAsync<ShopItem>();

            // 1) Create Mock Rooms (3 different types)
            var rooms = new List<Room>
            {
                new Room
                {
                    Name = "Deluxe Suite",
                    Status = RoomStatus.Available,
                    RoomType = RoomTypes.Large,
                    MaxOccupants = 3,
                    BasePrice = 1500,
                    EndDate = new DateTime(2026, 12, 31),
                    ImgUrl = ""
                },
                new Room
                {
                    Name = "Standard Room",
                    Status = RoomStatus.Available,
                    RoomType = RoomTypes.Medium,
                    MaxOccupants = 2,
                    BasePrice = 1000,
                    EndDate = new DateTime(2026, 12, 31),
                    ImgUrl = ""
                },
                new Room
                {
                    Name = "Cozy Corner",
                    Status = RoomStatus.Available,
                    RoomType = RoomTypes.Small,
                    MaxOccupants = 1,
                    BasePrice = 600,
                    EndDate = new DateTime(2026, 12, 31),
                    ImgUrl = ""
                }
            };

            foreach (var room in rooms)
            {
                await _db.InsertAsync(room);
            }

            // 2) Create Mock Customers (3 customers)
            var customers = new List<Customer>
            {
                new Customer
                {
                    Name = "John Smith",
                    TelephoneNum = "0812345678",
                    Email = "john.smith@email.com",
                    ImgUrl = ""
                },
                new Customer
                {
                    Name = "Emily Johnson",
                    TelephoneNum = "0823456789",
                    Email = "emily.johnson@email.com",
                    ImgUrl = ""
                },
                new Customer
                {
                    Name = "Michael Chen",
                    TelephoneNum = "0834567890",
                    Email = "michael.chen@email.com",
                    ImgUrl = ""
                }
            };

            foreach (var customer in customers)
            {
                await _db.InsertAsync(customer);
            }

            // 3) Create Mock Cats (3 cats)
            var cats = new List<Cat>
            {
                new Cat
                {
                    Name = "Whiskers",
                    Breed = "Persian",
                    Age = 3,
                    Gender = Gender.Male,
                    ImgUrl = ""
                },
                new Cat
                {
                    Name = "Luna",
                    Breed = "Siamese",
                    Age = 2,
                    Gender = Gender.Female,
                    ImgUrl = ""
                },
                new Cat
                {
                    Name = "Tiger",
                    Breed = "Bengal",
                    Age = 5,
                    Gender = Gender.Male,
                    ImgUrl = ""
                }
            };

            foreach (var cat in cats)
            {
                await _db.InsertAsync(cat);
            }

            // 4) Create Mock Shop Items (12 items: 6 Accessories, 4 Necessities, 2 Toys)
            var shopItems = new List<ShopItem>
            {
                // 1. Kawaii Existential Cats Blind Box
                new ShopItem
                {
                    Id = 1,
                    Name = "Kawaii Existential Cats Blind Box",
                    Description = "Small cats statue blind box",
                    ItemPrice = 1500.00,
                    ItemType = ItemType.Miscellaneous,
                    ItemStatus = ItemStatus.Available,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 2. Cat Pen Holder
                new ShopItem
                {
                    Id = 2,
                    Name = "Cat Pen Holder",
                    Description = "A must-have pen holder! (in a cat shape)",
                    ItemPrice = 300.00,
                    ItemType = ItemType.Miscellaneous,
                    ItemStatus = ItemStatus.Unavailable,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 3. Wooden Basket
                new ShopItem
                {
                    Id = 3,
                    Name = "Wooden Basket",
                    Description = "A basket for your cat to sit in!",
                    ItemPrice = 250.00,
                    ItemType = ItemType.Accessory,
                    ItemStatus = ItemStatus.Unavailable,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 4. Small Pet Select U.S. - Pelleted Pine Cat Litter
                new ShopItem
                {
                    Id = 4,
                    Name = "Small Pet Select U.S. - Pelleted Pine Cat Litter",
                    Description = "100% All-natural litter",
                    ItemPrice = 650.00,
                    ItemType = ItemType.Necessity,
                    ItemStatus = ItemStatus.Unavailable,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 5. Cute Pink Bow-tie
                new ShopItem
                {
                    Id = 5,
                    Name = "Cute Pink Bow-tie",
                    Description = "A cute Pink bow-tie accessory for your cat!",
                    ItemPrice = 120.00,
                    ItemType = ItemType.Accessory,
                    ItemStatus = ItemStatus.Unavailable,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 6. Heart Hand-knitted Beanie
                new ShopItem
                {
                    Id = 6,
                    Name = "Heart Hand-knitted Beanie",
                    Description = "Cute heart hand-knitted beanie",
                    ItemPrice = 135.00,
                    ItemType = ItemType.Accessory,
                    ItemStatus = ItemStatus.Unavailable,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 7. Sisal Ball Grinding Claw Kitty Stick
                new ShopItem
                {
                    Id = 7,
                    Name = "Sisal Ball Grinding Claw Kitty Stick",
                    Description = "Interactive cat toy",
                    ItemPrice = 85.00,
                    ItemType = ItemType.Toy,
                    ItemStatus = ItemStatus.Unavailable,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 8. Petstages Squeak Squeak Mouse Plush
                new ShopItem
                {
                    Id = 8,
                    Name = "Petstages Squeak Squeak Mouse Plush",
                    Description = "A mouse-shaped plush cat toy",
                    ItemPrice = 45.00,
                    ItemType = ItemType.Toy,
                    ItemStatus = ItemStatus.Unavailable,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 9. Royal Canin Weight Minceur
                new ShopItem
                {
                    Id = 9,
                    Name = "Royal Canin Weight Minceur",
                    Description = "Feline care nutrition, dry cat food",
                    ItemPrice = 1250.00,
                    ItemType = ItemType.Food,
                    ItemStatus = ItemStatus.Unavailable,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 10. Royal Canin Kitten Gravy Cat Food
                new ShopItem
                {
                    Id = 10,
                    Name = "Royal Canin Kitten Gravy Cat Food",
                    Description = "Pack of 6, 3 oz",
                    ItemPrice = 2350.00,
                    ItemType = ItemType.Food,
                    ItemStatus = ItemStatus.Unavailable,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
    
                // ---- Added "Service" Mock Data ----
    
                // 11. Full Cat Grooming & Spa
                new ShopItem
                {
                    Id = 11,
                    Name = "Full Cat Grooming & Spa Session",
                    Description = "Premium bath, blow-dry, nail trim, and ear cleaning service.",
                    ItemPrice = 850.00,
                    ItemType = ItemType.Service,
                    ItemStatus = ItemStatus.Available,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                },
                // 12. In-Room Vet Health Checkup
                new ShopItem
                {
                    Id = 12,
                    Name = "In-Room Routine Health Checkup",
                    Description = "A professional wellness exam by a licensed veterinarian directly in their cozy room.",
                    ItemPrice = 1200.00,
                    ItemType = ItemType.Service,
                    ItemStatus = ItemStatus.Available,
                    ImgUrl = "",
                    CreatedAt = DateTime.Now
                }
            };

            foreach (var item in shopItems)
            {
                await _db.InsertAsync(item);
            }

            // 5) Create Mock Bookings (3 bookings with different customers, cats, and rooms)
            // Date range: 15/05/2026 to 25/05/2026
            var bookings = new List<Booking>
            {
                new Booking
                {
                    RoomId = rooms[0].Id, // Deluxe Suite
                    CustomerId = customers[0].Id, // John Smith
                    StartDate = new DateTime(2026, 5, 15),
                    EndDate = new DateTime(2026, 5, 20),
                    DiscountId = null,
                    TotalPrice = 7500 // 5 nights * 1500
                },
                new Booking
                {
                    RoomId = rooms[1].Id, // Standard Room
                    CustomerId = customers[1].Id, // Emily Johnson
                    StartDate = new DateTime(2026, 5, 16),
                    EndDate = new DateTime(2026, 5, 23),
                    DiscountId = null,
                    TotalPrice = 7000 // 7 nights * 1000
                },
                new Booking
                {
                    RoomId = rooms[2].Id, // Cozy Corner
                    CustomerId = customers[2].Id, // Michael Chen
                    StartDate = new DateTime(2026, 5, 18),
                    EndDate = new DateTime(2026, 5, 25),
                    DiscountId = null,
                    TotalPrice = 4200 // 7 nights * 600
                }
            };

            // UNCOMMENT TO ADD BOOKING
            //foreach (var booking in bookings)
            //{
            //    await _db.InsertAsync(booking);
            //}

            // 6) Link Cats to Bookings using BookingCat table
            var bookingCatLinks = new List<BookingCat>
            {
                new BookingCat { BookingId = bookings[0].Id, CatId = cats[0].Id }, // John + Whiskers
                new BookingCat { BookingId = bookings[1].Id, CatId = cats[1].Id }, // Emily + Luna
                new BookingCat { BookingId = bookings[2].Id, CatId = cats[2].Id }  // Michael + Tiger
            };

            // UNCOMMENT TO LINK CATS TO BOOKINGS
            //foreach (var link in bookingCatLinks)
            //{
            //    await _db.InsertAsync(link);
            //}

            // 7) Add discount codes
            var discounts = new List<Discount>
            {
                // ---- EXPIRED COUPONS (Before 19/05/2026) ----
    
                // 1. Early Bird 2026 Promo (-฿150 flat)
                new Discount
                {
                    Id = 1,
                    Code = "EARLYCAT150",
                    Description = "Get ฿150 off early spring bookings.",
                    Amount = 150,
                    Quantity = 100,
                    UsedCount = 100, // Fully used up
                    ExpirationDate = new DateTime(2026, 4, 30), // Expired April 30, 2026
                    CreatedAt = new DateTime(2026, 3, 1)
                },
                // 2. Songkran Festival Special (-฿300 flat)
                new Discount
                {
                    Id = 2,
                    Code = "SONGKRAN300",
                    Description = "Special holiday discount for the water festival.",
                    Amount = 300,
                    Quantity = 50,
                    UsedCount = 42,
                    ExpirationDate = new DateTime(2026, 5, 15), // Expired May 15, 2026
                    CreatedAt = new DateTime(2026, 4, 10)
                },

                // ---- ACTIVE COUPONS (After 19/05/2026) ----
    
                // 3. Welcome New User Discount (-฿100 flat)
                new Discount
                {
                    Id = 3,
                    Code = "MEOWWELCOME",
                    Description = "Welcome discount for first-time cat hotel bookings.",
                    Amount = 100,
                    Quantity = 500,
                    UsedCount = 134,
                    ExpirationDate = new DateTime(2026, 12, 31), // Valid until end of year
                    CreatedAt = new DateTime(2026, 1, 1)
                },
                // 4. Mid-Year Flash Sale (-฿500 flat)
                new Discount
                {
                    Id = 4,
                    Code = "MIDYEAR500",
                    Description = "Huge mid-year saving event for premium suites.",
                    Amount = 500,
                    Quantity = 30,
                    UsedCount = 5,
                    ExpirationDate = new DateTime(2026, 6, 30), // Valid until June 30, 2026
                    CreatedAt = new DateTime(2026, 5, 15)
                },
                // 5. Cozy Corner Special Promotion (-฿50 flat)
                new Discount
                {
                    Id = 5,
                    Code = "COZYKITTY50",
                    Description = "Flat ฿50 discount on shop items and room upgrades.",
                    Amount = 50,
                    Quantity = 200,
                    UsedCount = 12,
                    ExpirationDate = new DateTime(2026, 8, 18), // Valid until August 18, 2026
                    CreatedAt = new DateTime(2026, 5, 1)
                }
            };

            foreach (var code in discounts)
            {
                await _db.InsertAsync(code);
            }

            System.Diagnostics.Debug.WriteLine("[SEED] Mock data created successfully!");
            System.Diagnostics.Debug.WriteLine($"[SEED] Rooms: {await _db.Table<Room>().CountAsync()}");
            System.Diagnostics.Debug.WriteLine($"[SEED] Customers: {await _db.Table<Customer>().CountAsync()}");
            System.Diagnostics.Debug.WriteLine($"[SEED] Cats: {await _db.Table<Cat>().CountAsync()}");
            System.Diagnostics.Debug.WriteLine($"[SEED] Shop Items: {await _db.Table<ShopItem>().CountAsync()}");
            //System.Diagnostics.Debug.WriteLine($"[SEED] Bookings: {await _db.Table<Booking>().CountAsync()}");
        }
    }
}