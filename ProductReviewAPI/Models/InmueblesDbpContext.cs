using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProductReviewAPI.Models;

public partial class InmueblesDbpContext : DbContext
{
    public InmueblesDbpContext()
    {
    }

    public InmueblesDbpContext(DbContextOptions<InmueblesDbpContext> options)
        : base(options)
    {
    }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }

    public virtual DbSet<ProductComment> ProductComments { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<User> Users { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailVer__3214EC0740527970");

            entity.ToTable("EmailVerification");

            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Expiration)
                .HasPrecision(0)
                .HasDefaultValueSql("(dateadd(minute,(5),getdate()))");
        });

        modelBuilder.Entity<ProductComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductC__3214EC07DFDFBF21");

            entity.Property(e => e.Comment).IsUnicode(false);
            entity.Property(e => e.Prediction).IsUnicode(false);
            entity.Property(e => e.Product).IsUnicode(false);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC078BF8AF77");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("(dateadd(day,(30),getdate()))")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC070C3FC56A");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534A476A306").IsUnique();

            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasDefaultValue("User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
