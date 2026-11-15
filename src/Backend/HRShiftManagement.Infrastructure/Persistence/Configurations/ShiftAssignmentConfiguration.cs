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
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.ToTable("ShiftAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.WorkDate)
            .HasColumnType("date");

        // Lien ket voi ca lam viec
        builder.HasOne(a => a.Shift)
            .WithMany()
            .HasForeignKey(a => a.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lien ket voi ho so nhan vien
        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tim kiem lich theo ca va ngay
        builder.HasIndex(a => new
        {
            a.ShiftId,
            a.WorkDate
        });

        // Tim kiem lich theo nhan vien va ngay
        builder.HasIndex(a => new
        {
            a.EmployeeId,
            a.WorkDate
        });
    }
}