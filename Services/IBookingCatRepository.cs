using CatHotel.Models;

namespace CatHotel.Services;

public interface IBookingCatRepository
{
    Task<int> AddCatToBookingAsync(int bookingId, int catId);
    Task<int> RemoveCatFromBookingAsync(int bookingId, int catId);
    Task<bool> IsCatInBookingAsync(int bookingId, int catId);
    Task<BookingCat?> GetBookingCatByIdAsync(int bookingCatId);
    Task<int> UpdateCatInBookingAsync(int bookingCatId, int newCatId);
}