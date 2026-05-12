using CatHotel.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CatHotel.Services;

/// <summary>
/// Local cart ที่อยู่ใน memory ระหว่าง session
/// </summary>
public class CartService : INotifyPropertyChanged
{
    private static CartService? _instance;
    public static CartService Instance => _instance ??= new CartService();

    public List<CartEntry> Items { get; } = new();

    public int Count => Items.Sum(c => c.Quantity);
    public double Total => Items.Sum(c => c.Subtotal);

    public void Add(ShopItem item, int qty = 1)
    {
        var existing = Items.FirstOrDefault(c => c.Item.Id == item.Id);
        if (existing != null)
            existing.Quantity += qty;
        else
            Items.Add(new CartEntry { Item = item, Quantity = qty });
        
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Total));
    }

    public void Remove(CartEntry entry)
    {
        Items.Remove(entry);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Total));
    }

    public void Clear()
    {
        Items.Clear();
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Total));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class CartEntry
{
    public ShopItem Item { get; set; } = new();
    public int Quantity { get; set; }
    public double Subtotal => Item.ItemPrice * Quantity;
    public string SubtotalDisplay => $"฿{Subtotal:N0}";
    public string PriceDisplay => $"฿{Item.ItemPrice:N0}";
}
