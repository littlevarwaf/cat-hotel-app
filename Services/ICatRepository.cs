using CatHotel.Models;

namespace CatHotel.Services;

public interface ICatRepository
{
    Task<List<Cat>> GetAllCatsAsync();
    Task<Cat?> GetCatByIdAsync(int id);
    Task<List<Cat>> GetCatsByBookingIdAsync(int bookingId);
    Task<int> AddCatAsync(Cat cat);
    Task<int> UpdateCatAsync(Cat cat);
    Task<int> DeleteCatAsync(Cat cat);
}