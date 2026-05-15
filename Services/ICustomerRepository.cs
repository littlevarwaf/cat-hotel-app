using CatHotel.Models;

namespace CatHotel.Services;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(int id);
    Task<int> AddCustomerAsync(Customer customer);
    Task<int> UpdateCustomerAsync(Customer customer);
    Task<int> DeleteCustomerAsync(Customer customer);
}