using SQLite;

namespace CatHotel.Models;

[Table("BookingCats")]
public class BookingCat
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    [NotNull]
    public int BookingId { get; set; }

    [Indexed]
    [NotNull]
    public int CatId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [Ignore]
    public Booking Booking { get; set; }

    [Ignore]
    public Cat Cat { get; set; }

    // ---- Constructors ----
    public BookingCat()
    {
    }

    public BookingCat(int bookingId, int catId)
    {
        BookingId = bookingId;
        CatId = catId;
        CreatedAt = DateTime.Now;
    }
}