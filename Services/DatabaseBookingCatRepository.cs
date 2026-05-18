using CatHotel.Models;

namespace CatHotel.Services;

public class DatabaseBookingCatRepository : IBookingCatRepository
{
    private CatHotel.Services.DatabaseService Db => App.Database;

    public async Task<int> AddCatToBookingAsync(int bookingId, int catId)
    {
        await Db.InitializeAsync();
        var bookingCat = new BookingCat(bookingId, catId);
        await Db.Db.InsertAsync(bookingCat);
        return bookingCat.Id;
    }

    public async Task<int> RemoveCatFromBookingAsync(int bookingId, int catId)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<BookingCat>()
            .DeleteAsync(bc => bc.BookingId == bookingId && bc.CatId == catId);
    }

    public async Task<bool> IsCatInBookingAsync(int bookingId, int catId)
    {
        await Db.InitializeAsync();
        var exists = await Db.Db.Table<BookingCat>()
            .Where(bc => bc.BookingId == bookingId && bc.CatId == catId)
            .FirstOrDefaultAsync();
        return exists != null;
    }

    public async Task<BookingCat?> GetBookingCatByIdAsync(int bookingCatId)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<BookingCat>()
            .Where(bc => bc.Id == bookingCatId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> UpdateCatInBookingAsync(int bookingCatId, int newCatId)
    {
        await Db.InitializeAsync();
        var bookingCat = await GetBookingCatByIdAsync(bookingCatId);
        if (bookingCat == null)
            throw new InvalidOperationException($"BookingCat with ID {bookingCatId} not found.");

        bookingCat.CatId = newCatId;
        return await Db.Db.UpdateAsync(bookingCat);
    }
}