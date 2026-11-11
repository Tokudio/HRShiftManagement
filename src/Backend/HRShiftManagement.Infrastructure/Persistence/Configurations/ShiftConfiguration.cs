using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using HRShiftManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRShiftManagement.Infrastructure.Persistence.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        // Ten bang trong SQL Server
        builder.ToTable("Shifts");

        // Khoa chinh
        builder.HasKey(s => s.Id);

        // Ten ca bat buoc, toi da 100 ky tu
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Khong cho phep trung ten ca
        builder.HasIndex(s => s.Name)
            .IsUnique();

        // Gio bat dau
        builder.Property(s => s.StartTime)
            .HasColumnType("time(0)");

        // Gio ket thuc
        builder.Property(s => s.EndTime)
            .HasColumnType("time(0)");

        // Rang buoc du lieu
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Shifts_Employees",
                "[MinEmployees] >= 1 AND " +
                "[MaxEmployees] >= [MinEmployees]");

            t.HasCheckConstraint(
                "CK_Shifts_Time",
                "[StartTime] <> [EndTime]");
        });

        // Khong xoa ca neu da co lich phan ca
        builder.HasMany(s => s.Assignments)
            .WithOne(a => a.Shift)
            .HasForeignKey(a => a.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
