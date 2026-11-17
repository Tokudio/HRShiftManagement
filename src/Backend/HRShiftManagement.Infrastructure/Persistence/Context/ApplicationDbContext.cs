
using HRShiftManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRShiftManagement.Infrastructure.Persistence.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ============================================
    // Quan ly tai khoan
    // ============================================

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens
        => Set<RefreshToken>();


    // ============================================
    // US 2.1 - Quan ly ca lam viec
    // ============================================

    public DbSet<Shift> Shifts
        => Set<Shift>();


    // ============================================
    // US 2.2 - Phan nhan vien vao ca
    // ============================================

    public DbSet<ShiftAssignment> ShiftAssignments
        => Set<ShiftAssignment>();


    // ============================================
    // US 1.5 & US 1.6 - Quan ly ho so nhan vien
    // ============================================

    public DbSet<EmployeeProfile> EmployeeProfiles
        => Set<EmployeeProfile>();


    // ============================================
    // Cau hinh cac Entity
    // ============================================

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tu dong nap cac Entity Configuration
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly
        );
    }
}
