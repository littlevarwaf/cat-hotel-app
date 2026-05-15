using SQLite;

namespace CatHotel.Models;

[Table("BookingCats")]
public class BookingCat
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int BookingId { get; set; }

    [Indexed]
    public int CatId { get; set; }
}
