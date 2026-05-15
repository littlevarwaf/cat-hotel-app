using CatHotel.Models;

namespace CatHotel.Services;

public class DatabaseCustomerRepository : ICustomerRepository
{
    private CatHotel.Services.DatabaseService Db => App.Database;

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Customer>().ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        await Db.InitializeAsync();
        return await Db.Db.Table<Customer>()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> AddCustomerAsync(Customer customer)
    {
        await Db.InitializeAsync();
        await Db.Db.InsertAsync(customer);
        return customer.Id;
    }

    public async Task<int> UpdateCustomerAsync(Customer customer)
    {
        await Db.InitializeAsync();
        return await Db.Db.UpdateAsync(customer);
    }

    public async Task<int> DeleteCustomerAsync(Customer customer)
    {
        await Db.InitializeAsync();
        return await Db.Db.DeleteAsync(customer);
    }
}