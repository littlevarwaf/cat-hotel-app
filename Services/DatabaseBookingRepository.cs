using CatHotel.Models;

namespace CatHotel.Services;

public class DatabaseBookingRepository : IBookingRepository
{
    private CatHotel.Services.DatabaseService Db => App.Database;

    // ---- Booking Read ----

    public async Task<List<Booking>> GetAllBookingsAsync()
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Booking>().ToListAsync();
    }

    public async Task<Booking?> GetBookingByIdAsync(int id)
    {
        await Db.InitializeAsync();
        var booking = await Db.Db.Table<Booking>()
            .Where(b => b.Id == id)
            .FirstOrDefaultAsync();

        if (booking != null)
        {
            // Load related data
            booking.Room = await Db.Db.Table<Room>()
                .Where(r => r.Id == booking.RoomId)
                .FirstOrDefaultAsync();

            booking.Customer = await Db.Db.Table<Customer>()
                .Where(c => c.Id == booking.CustomerId)
                .FirstOrDefaultAsync();
        }

        return booking;
    }

    public async Task<List<Booking>> GetBookingsByCustomerIdAsync(int customerId)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Booking>()
            .Where(b => b.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetBookingsByRoomIdAsync(int roomId)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Booking>()
            .Where(b => b.RoomId == roomId)
            .ToListAsync();
    }

    // ---- BookingItem Read ----

    public async Task<List<BookingItem>> GetBookingItemsByBookingIdAsync(int bookingId)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<BookingItem>()
            .Where(bi => bi.BookingId == bookingId)
            .ToListAsync();
    }

    public async Task<BookingItem?> GetBookingItemByIdAsync(int id)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<BookingItem>()
            .Where(bi => bi.Id == id)
            .FirstOrDefaultAsync();
    }

    // ---- BookingCat Read ----

    public async Task<List<BookingCat>> GetBookingCatsByBookingIdAsync(int bookingId)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<BookingCat>()
            .Where(bc => bc.BookingId == bookingId)
            .ToListAsync();
    }

    // ---- Booking Write ----

    public async Task<int> AddBookingAsync(Booking booking)
    {
        await Db.InitializeAsync();
        await Db.Db.InsertAsync(booking);
        return booking.Id;
    }

    public async Task<int> UpdateBookingAsync(Booking booking)
    {
        await Db.InitializeAsync();
        return await Db.Db.UpdateAsync(booking);
    }

    public async Task<int> DeleteBookingAsync(Booking booking)
    {
        await Db.InitializeAsync();
        return await Db.Db.DeleteAsync(booking);
    }

    // ---- BookingItem Write ----

    public async Task<int> AddBookingItemAsync(BookingItem item)
    {
        await Db.InitializeAsync();
        await Db.Db.InsertAsync(item);
        return item.Id;
    }

    public async Task<int> UpdateBookingItemAsync(BookingItem item)
    {
        await Db.InitializeAsync();
        return await Db.Db.UpdateAsync(item);
    }

    public async Task<int> DeleteBookingItemAsync(BookingItem item)
    {
        await Db.InitializeAsync();
        return await Db.Db.DeleteAsync(item);
    }

    // ---- BookingCat Write ----

    public async Task<int> AddBookingCatAsync(BookingCat bookingCat)
    {
        await Db.InitializeAsync();
        await Db.Db.InsertAsync(bookingCat);
        return bookingCat.Id;
    }

    public async Task<int> DeleteBookingCatAsync(BookingCat bookingCat)
    {
        await Db.InitializeAsync();
        return await Db.Db.DeleteAsync(bookingCat);
    }
}