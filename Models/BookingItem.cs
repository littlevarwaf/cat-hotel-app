using SQLite;

namespace CatHotel.Models;

[Table("BookingItems")]
public class BookingItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public int BookingId { get; set; }

    [NotNull]
    public int ItemId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties (optional, for convenience)
    [Ignore]
    public Booking Booking { get; set; }
    [Ignore]
    public Cat Cat { get; set; }
    public BookingItem()
    {
    }
    public BookingItem(int bookingId, int itemId)
    {
        BookingId = bookingId;
        ItemId = itemId;
        CreatedAt = DateTime.Now;
    }
}