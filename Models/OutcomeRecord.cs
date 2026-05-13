using SQLite;

namespace CatHotel.Models;

[Table("OutcomeRecords")]
public class OutcomeRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public double Amount { get; set; }

    public string Note { get; set; } = string.Empty;

    [NotNull]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
