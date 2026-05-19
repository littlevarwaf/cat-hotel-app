using CatHotel.Models;

namespace CatHotel.Services;

public interface IBookingRepository
{
    Task<List<Booking>> GetAllBookingsAsync();
    Task<Booking?> GetBookingByIdAsync(int id);
    Task<List<Booking>> GetBookingsByCustomerIdAsync(int customerId);
    Task<List<Booking>> GetBookingsByRoomIdAsync(int roomId);
    Task<List<BookingItem>> GetBookingItemsByBookingIdAsync(int bookingId);
    Task<BookingItem?> GetBookingItemByIdAsync(int id);
    Task<List<BookingCat>> GetBookingCatsByBookingIdAsync(int bookingId);
    Task<int> AddBookingAsync(Booking booking);
    Task<int> AddBookingItemAsync(BookingItem item);
    Task<int> AddBookingCatAsync(BookingCat bookingCat);
    Task<int> UpdateBookingAsync(Booking booking);
    Task<int> UpdateBookingItemAsync(BookingItem item);
    Task<int> DeleteBookingAsync(Booking booking);
    Task<int> DeleteBookingItemAsync(BookingItem item);
    Task<int> DeleteBookingCatAsync(BookingCat bookingCat);

    /// <summary>
    /// Gets all bookings for a specific date range with Room objects loaded.
    /// </summary>
    Task<List<Booking>> GetBookingsForDateRangeWithRoomsAsync(DateTime startDate, DateTime endDate);
}