using CatHotel.Models;

namespace CatHotel.Services;

public class DatabaseCatRepository : ICatRepository
{
    private CatHotel.Services.DatabaseService Db => App.Database;

    public async Task<List<Cat>> GetAllCatsAsync()
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Cat>().ToListAsync();
    }

    public async Task<Cat?> GetCatByIdAsync(int id)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Cat>()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Cat>> GetCatsByBookingIdAsync(int bookingId)
    {
        await Db.InitializeAsync();

        // Get all BookingCat entries for this booking
        var bookingCats = await Db.Db.Table<BookingCat>()
            .Where(bc => bc.BookingId == bookingId)
            .ToListAsync();

        // Get the actual Cat objects
        var catIds = bookingCats.Select(bc => bc.CatId).ToList();

        if (catIds.Count == 0)
            return new List<Cat>();

        return await Db.Db.Table<Cat>()
            .Where(c => catIds.Contains(c.Id))
            .ToListAsync();
    }

    public async Task<int> AddCatAsync(Cat cat)
    {
        await Db.InitializeAsync();
        await Db.Db.InsertAsync(cat);
        return cat.Id;
    }

    public async Task<int> UpdateCatAsync(Cat cat)
    {
        await Db.InitializeAsync();
        return await Db.Db.UpdateAsync(cat);
    }

    public async Task<int> DeleteCatAsync(Cat cat)
    {
        await Db.InitializeAsync();
        return await Db.Db.DeleteAsync(cat);
    }
}