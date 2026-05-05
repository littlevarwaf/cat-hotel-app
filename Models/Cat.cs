using SQLite;

namespace CatHotel.Models;

[Table("Cats")]
public class Cat
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; }

    [NotNull]
    public string Breed { get; set; }

    [NotNull]
    public int Age { get; set; }

    [NotNull]
    public Gender Gender { get; set; }

    [NotNull]
    public string ImgUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // ---- Constructors ----   
    public Cat()
    {
    }

    public Cat(string name, string breed, string imgUrl, Gender gender = Gender.Unknown)
    {
        Name = name;
        Breed = breed;
        ImgUrl = imgUrl;
        Gender = gender;
        CreatedAt = DateTime.Now;
    }
}