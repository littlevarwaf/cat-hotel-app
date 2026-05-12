using SQLite;

namespace CatHotel.Models;

[Table("ShopItems")]
public class ShopItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; }

    [NotNull]
    public string Description { get; set; }

    [NotNull]
    public double ItemPrice { get; set; }

    [NotNull]
    public ItemType ItemType { get; set; }

    [NotNull]
    public string ImgUrl { get; set; }

    [Ignore]
    public bool IsNew => false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ---- Constructors ----
    public ShopItem()
    {
    }

    public ShopItem(string name, string description, double price, ItemType itemType, string imgUrl)
    {
        Name = name;
        Description = description;
        ItemPrice = price;
        ItemType = itemType;
        ImgUrl = imgUrl;
        CreatedAt = DateTime.Now;
    }
}