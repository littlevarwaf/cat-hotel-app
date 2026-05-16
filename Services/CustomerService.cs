using CatHotel.Models;

namespace CatHotel.Services;

public static class CustomerService
{
    public static event EventHandler<CustomerEventArgs>? CustomerAdded;
    public static event EventHandler<CustomerEventArgs>? CustomerUpdated;
    public static event EventHandler<CustomerEventArgs>? CustomerDeleted;

    public static void NotifyCustomerAdded(Customer customer)
    {
        CustomerAdded?.Invoke(null, new CustomerEventArgs { Customer = customer });
    }

    public static void NotifyCustomerUpdated(Customer customer)
    {
        CustomerUpdated?.Invoke(null, new CustomerEventArgs { Customer = customer });
    }

    public static void NotifyCustomerDeleted(Customer customer)
    {
        CustomerDeleted?.Invoke(null, new CustomerEventArgs { Customer = customer });
}
}

public class CustomerEventArgs : EventArgs
{
    public Customer? Customer { get; set; }
}
