namespace NativeAotWebApplication1.EF;

using Microsoft.EntityFrameworkCore;

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string lastName { get; set; }

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}

public class Address
{
    public int Id { get; set; }
    public string SomethingAddress { get; set; }

    public int PersonId { get; set; }
    public Person? Person { get; set; }
}

public class MySQLiteContext : DbContext
{
    public DbSet<Person> People { get; }
    public DbSet<Address> Addresses { get; }

    public MySQLiteContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>();
        modelBuilder.Entity<Address>(addresses =>
        {
            addresses
                .HasOne<Person>()
                .WithMany(person => person.Addresses)
                .HasForeignKey(address => address.PersonId);
        });
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(); //TODO <- Connection string could go here, or in appsettings
        base.OnConfiguring(optionsBuilder);
    }
}
