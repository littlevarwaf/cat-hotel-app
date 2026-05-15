using CatHotel.Models;

namespace CatHotel.Services;

public static class CatService
{
    public static event EventHandler<CatEventArgs>? CatAdded;
    public static event EventHandler<CatEventArgs>? CatUpdated;
    public static event EventHandler<CatEventArgs>? CatDeleted;

    public static void NotifyCatAdded(Cat cat) =>
        CatAdded?.Invoke(null, new CatEventArgs { Cat = cat });

    public static void NotifyCatUpdated(Cat cat) =>
        CatUpdated?.Invoke(null, new CatEventArgs { Cat = cat });

    public static void NotifyCatDeleted(Cat cat) =>
        CatDeleted?.Invoke(null, new CatEventArgs { Cat = cat });
}

public class CatEventArgs : EventArgs
{
    public Cat? Cat { get; set; }
}
