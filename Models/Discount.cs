using SQLite;

namespace CatHotel.Models;

[Table("Discounts")]
public class Discount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; }

    [NotNull]
    public double Percentage { get; set; }

    [NotNull]
    public int Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [NotNull]
    public DateTime ExpirationDate {  get; set; }

    // ---- Constructors ----
    public Discount()
    {
    }

    public Discount(string name, double percentage, int amount, DateTime expirationDate)
    {
        Name = name;
        Percentage = percentage;
        ExpirationDate = expirationDate;
        CreatedAt = DateTime.Now;
    }
}