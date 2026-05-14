using SQLite;

namespace CatHotel.Models;

[Table("Discounts")]
public class Discount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Code { get; set; }

    public string Description { get; set; } = string.Empty;

    [NotNull]
    public int Amount { get; set; }

    public int Quantity { get; set; }

    public int UsedCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [NotNull]
    public DateTime ExpirationDate {  get; set; }

    // ---- Constructors ----
    public Discount()
    {
    }

    public Discount(string code, string description, int amount, int quantity, DateTime expirationDate)
    {
        Code = code;
        Description = description;
        Amount = amount;
        Quantity = quantity;
        ExpirationDate = expirationDate;
        CreatedAt = DateTime.Now;
    }
}