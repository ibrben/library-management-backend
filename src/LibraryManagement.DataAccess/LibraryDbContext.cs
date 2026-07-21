using LibraryManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.DataAccess;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BorrowTransaction> BorrowTransactions => Set<BorrowTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
