using SQLite;

namespace CatHotel.Models;

[Table("Bookings")]
public class Booking
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public int RoomId { get; set; }

    [NotNull]
    public int CustomerId { get; set; }

    [Ignore]//[NotNull]
    public List<Cat> Cats { get; set; } = new List<Cat>();

    [NotNull]
    public double TotalPrice { get; set; }

    [Indexed]
    public int? DiscountId { get; set; } // nullable since discount is optional

    [NotNull]
    public DateTime StartDate { get; set; }

    [NotNull]
    public DateTime EndDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties (optional, for convenience)
    [Ignore]
    public Room Room { get; set; }

    [Ignore]
    public Customer Customer { get; set; }

    [Ignore]
    public Discount Discount { get; set; }

    // ---- Constructors ----
    public Booking()
    {
    }

    public Booking(int roomId, int customerId, DateTime startDate, DateTime endDate, int? discountId = null)
    {
        RoomId = roomId;
        CustomerId = customerId;
        StartDate = startDate;
        EndDate = endDate;
        DiscountId = discountId;
        CreatedAt = DateTime.Now;
    }
}