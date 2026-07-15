using LibraryManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.DataAccess.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(book => book.Id);
        builder.Property(book => book.Isbn).HasColumnName("ISBN").HasMaxLength(20).IsRequired();
        builder.Property(book => book.Title).HasMaxLength(300).IsRequired();
        builder.Property(book => book.Author).HasMaxLength(200).IsRequired();
        builder.Property(book => book.Publisher).HasMaxLength(200);
        builder.Property(book => book.Category).HasMaxLength(100);
        builder.Property(book => book.Shelf).HasMaxLength(100).IsRequired();
        builder.Property(book => book.AvailabilityStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(book => book.Isbn).IsUnique();
        builder.HasIndex(book => book.Title);
        builder.HasIndex(book => book.Author);
    }
}
