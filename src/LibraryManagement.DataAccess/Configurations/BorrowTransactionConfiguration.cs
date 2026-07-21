using LibraryManagement.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.DataAccess.Configurations;

internal sealed class BorrowTransactionConfiguration : IEntityTypeConfiguration<BorrowTransaction>
{
    public void Configure(EntityTypeBuilder<BorrowTransaction> builder)
    {
        builder.ToTable("BorrowTransactions");
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasOne(transaction => transaction.User)
            .WithMany(user => user.BorrowTransactions)
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(transaction => transaction.Book)
            .WithMany(book => book.BorrowTransactions)
            .HasForeignKey(transaction => transaction.BookId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(transaction => new { transaction.UserId, transaction.Status });
        builder.HasIndex(transaction => new { transaction.BookId, transaction.Status });
    }
}
