using SQLite;

namespace CatHotel.Models;

[Table("Customers")]
public class Customer
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [NotNull]
    public string Name { get; set; }
    [NotNull]
    public string TelephoneNum { get; set; }
    [NotNull]
    public string Email { get; set; }
    public string LineId { get; set; }
    [NotNull]
    public string ImgUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ---- Constructors ----
    public Customer()
    {
    }

    public Customer(string name, string telephoneNum, string email, string lineid, string imgUrl)
    {
        Name = name;
        TelephoneNum = telephoneNum;
        Email = email;
        LineId = lineid;
        ImgUrl = imgUrl;
        CreatedAt = DateTime.Now;
    }
}
