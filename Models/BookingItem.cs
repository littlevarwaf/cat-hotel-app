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
    [NotNull]
    public int Quantity { get; set; } = 1;

    [NotNull]
    public double UnitPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties (optional, for convenience)
    [Ignore]
    public Booking Booking { get; set; }
    [Ignore]
    public Cat Cat { get; set; }
    public BookingItem()
    {
    }
    public BookingItem(int bookingId, int itemId, int quantity = 1)
    {
        BookingId = bookingId;
        ItemId = itemId;
        Quantity = quantity <= 0 ? 1 : quantity;
        CreatedAt = DateTime.Now;
    }
}