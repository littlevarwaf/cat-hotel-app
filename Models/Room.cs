using SQLite;

namespace CatHotel.Models;
[Table("Rooms")]
public class Room
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; }

    [NotNull]
    public RoomStatus Status { get; set; }

    [NotNull]
    public RoomTypes RoomType { get; set; }

    [NotNull]
    public int MaxOccupants { get; set; } = 0;

    [NotNull]
    public double BasePrice { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Now;

    public DateTime EndDate { get; set; }

    [NotNull]
    public string ImgUrl { get; set; }

    // ---- Constructors ----
    public Room()
    {
    }
    public Room(string name, RoomStatus status, RoomTypes roomType, int maxOccupants, double basePrice, DateTime endDate, string imgUrl)
    {
        Name = name;
        Status = status;
        RoomType = roomType;
        MaxOccupants = maxOccupants;
        BasePrice = basePrice;
        StartDate = DateTime.Now;
        EndDate = endDate;
        ImgUrl = imgUrl;
    }
}