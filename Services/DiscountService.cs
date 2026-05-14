using CatHotel.Models;

namespace CatHotel.Services;

public static class DiscountService
{
    public static event EventHandler<DiscountEventArgs>? DiscountAdded;
    public static event EventHandler<DiscountEventArgs>? DiscountUpdated;
    public static event EventHandler<DiscountEventArgs>? DiscountDeleted;

    public static void NotifyDiscountAdded(Discount discount)
    {
        DiscountAdded?.Invoke(null, new DiscountEventArgs { Discount = discount });
    }

    public static void NotifyDiscountUpdated(Discount discount)
    {
        DiscountUpdated?.Invoke(null, new DiscountEventArgs { Discount = discount });
    }

    public static void NotifyDiscountDeleted(Discount discount)
    {
        DiscountDeleted?.Invoke(null, new DiscountEventArgs { Discount = discount });
    }
}

public class DiscountEventArgs : EventArgs
{
    public Discount? Discount { get; set; }
}