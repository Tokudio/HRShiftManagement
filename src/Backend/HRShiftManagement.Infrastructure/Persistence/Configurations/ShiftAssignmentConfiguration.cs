using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using HRShiftManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRShiftManagement.Infrastructure.Persistence.Configurations;

public class ShiftAssignmentConfiguration
    : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(
        EntityTypeBuilder<ShiftAssignment> builder)
    {
        // Ten bang
        builder.ToTable("ShiftAssignments");

        // Khoa chinh
        builder.HasKey(a => a.Id);

        // Ngay lam viec
        builder.Property(a => a.WorkDate)
            .HasColumnType("date");

        // Lien ket voi nhan vien
        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Chi muc ho tro tim lich theo ca va ngay
        builder.HasIndex(a => new
        {
            a.ShiftId,
            a.WorkDate
        });
    }
}
