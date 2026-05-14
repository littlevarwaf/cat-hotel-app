using CatHotel.Models;

namespace CatHotel.Services;

public static class OutcomeService
{
    public static event EventHandler<OutcomeEventArgs>? OutcomeAdded;
    public static event EventHandler<OutcomeEventArgs>? OutcomeUpdated;
    public static event EventHandler<OutcomeEventArgs>? OutcomeDeleted;

    public static void NotifyOutcomeAdded(OutcomeRecord outcome)
    {
        OutcomeAdded?.Invoke(null, new OutcomeEventArgs { Outcome = outcome });
    }

    public static void NotifyOutcomeUpdated(OutcomeRecord outcome)
    {
        OutcomeUpdated?.Invoke(null, new OutcomeEventArgs { Outcome = outcome });
    }

    public static void NotifyOutcomeDeleted(OutcomeRecord outcome)
    {
        OutcomeDeleted?.Invoke(null, new OutcomeEventArgs { Outcome = outcome });
    }
}

public class OutcomeEventArgs : EventArgs
{
    public OutcomeRecord? Outcome { get; set; }
}