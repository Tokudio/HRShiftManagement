using HRShiftManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRShiftManagement.Infrastructure.Persistence.Configurations;

public class EmployeeProfileConfiguration
    : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(
        EntityTypeBuilder<EmployeeProfile> builder)
    {
        // 1. Ten bang trong SQL Server
        builder.ToTable("EmployeeProfiles");

        // 2. Khoa chinh
        builder.HasKey(e => e.Id);

        // 3. Ma nhan vien khong duoc trung
        builder.HasIndex(e => e.EmployeeCode)
            .IsUnique();

        // 4. Mot tai khoan chi lien ket mot ho so
        builder.HasIndex(e => e.UserId)
            .IsUnique();

        // 5. Ma nhan vien
        builder.Property(e => e.EmployeeCode)
            .IsRequired()
            .HasMaxLength(24);

        // 6. Ho va ten
        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(200);

        // 7. So dien thoai
        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        // 8. Chuc vu
        builder.Property(e => e.Position)
            .IsRequired()
            .HasMaxLength(100);

        // 9. Phong ban
        builder.Property(e => e.Department)
            .IsRequired()
            .HasMaxLength(100);

        // 10. Email
        builder.Property(e => e.Email)
            .HasMaxLength(256);

        // 11. Dia chi
        builder.Property(e => e.Address)
            .HasMaxLength(300);

        // 12. Ghi chu
        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        // 13. Trang thai hoat dong
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        // 14. Lien ket ho so voi tai khoan User
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
