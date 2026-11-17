using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HRShiftManagement.Application.DTOs.Employees;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Persistence.Context;

using Microsoft.EntityFrameworkCore;

namespace HRShiftManagement.Infrastructure.Services;

public class EmployeeProfileService : IEmployeeProfileService
{
    private readonly ApplicationDbContext _db;

    public EmployeeProfileService(ApplicationDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // US 1.5 - 1. Lay danh sach ho so nhan vien
    // =====================================================
    public async Task<List<EmployeeProfileResponse>> GetAllAsync(
        CancellationToken ct = default)
    {
        var profiles = await _db.EmployeeProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p =>
                p.UserId == null ||
                (p.User != null &&
                 p.User.Role == UserRole.Employee))
            .OrderBy(p => p.FullName)
            .ToListAsync(ct);

        // Lay tai khoan Employee cu chua co ho so
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Employee)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

        var linked = profiles
            .Where(p => p.UserId.HasValue)
            .Select(p => p.UserId!.Value)
            .ToHashSet();

        var result = profiles
            .Select(ToResponse)
            .ToList();

        // Bo sung nhan vien da co tai khoan
        // nhung chua co EmployeeProfile
        result.AddRange(
            users
                .Where(u => !linked.Contains(u.Id))
                .Select(FromUser)
        );

        return result
            .OrderBy(x => x.FullName)
            .ToList();
    }

    // =====================================================
    // US 1.5 - 2. Xem chi tiet ho so nhan vien
    // =====================================================
    public async Task<EmployeeProfileResponse?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var profile = await _db.EmployeeProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (profile != null)
        {
            if (profile.UserId == null ||
                profile.User?.Role == UserRole.Employee)
            {
                return ToResponse(profile);
            }

            return null;
        }

        // Ho tro tai khoan Employee chua co ho so
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == id &&
                     u.Role == UserRole.Employee,
                ct
            );

        return user == null
            ? null
            : FromUser(user);
    }

    // =====================================================
    // US 1.5 - 3. Them ho so nhan vien
    // =====================================================
    public async Task<EmployeeProfileResponse> CreateAsync(
        EmployeeProfileRequest request,
        CancellationToken ct = default)
    {
        // Kiem tra du lieu dau vao
        Validate(request);

        var email = NormalizeEmail(request.Email);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        // Kiem tra email trung trong ho so
        await EnsureEmailNotUsedAsync(
            email, null, ct);

        // Tim tai khoan Employee co email tuong ung
        var user = await FindAccountAsync(email, ct);

        if (user != null &&
            await _db.EmployeeProfiles.AnyAsync(
                p => p.UserId == user.Id, ct))
        {
            throw new InvalidOperationException(
                "Nhan vien nay da co ho so.");
        }

        // Neu da co tai khoan thi dung ID tai khoan
        // Neu chua co thi tao ID moi
        var id = user?.Id ?? Guid.NewGuid();

        var profile = new EmployeeProfile
        {
            Id = id,
            EmployeeCode = MakeCode(id),
            CreatedAt = DateTime.UtcNow
        };

        // Gan thong tin vao ho so
        Apply(profile, request, email);

        // Lien ket tai khoan neu co
        profile.UserId = user?.Id;

        if (user != null)
        {
            SyncAccount(user, profile);
        }

        _db.EmployeeProfiles.Add(profile);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return (await GetByIdAsync(profile.Id, ct))!;
    }

    // =====================================================
    // US 1.6 - 4. Cap nhat ho so nhan vien
    // =====================================================
    public async Task<EmployeeProfileResponse> UpdateAsync(
        Guid id,
        EmployeeProfileRequest request,
        CancellationToken ct = default)
    {
        Validate(request);

        var email = NormalizeEmail(request.Email);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        var profile = await _db.EmployeeProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        // Neu la tai khoan Employee cu chua co ho so
        // thi tao ho so cho tai khoan do
        if (profile == null)
        {
            var legacyUser = await _db.Users
                .FirstOrDefaultAsync(
                    u => u.Id == id &&
                         u.Role == UserRole.Employee,
                    ct
                );

            if (legacyUser == null)
            {
                throw new KeyNotFoundException(
                    "Khong tim thay ho so nhan vien.");
            }

            profile = new EmployeeProfile
            {
                Id = legacyUser.Id,
                UserId = legacyUser.Id,
                User = legacyUser,
                EmployeeCode = MakeCode(legacyUser.Id),
                CreatedAt = DateTime.UtcNow
            };

            _db.EmployeeProfiles.Add(profile);
        }

        // Kiem tra vai tro cua tai khoan lien ket
        if (profile.User != null &&
            profile.User.Role != UserRole.Employee)
        {
            throw new KeyNotFoundException(
                "Khong tim thay ho so nhan vien.");
        }

        // Kiem tra email trung
        await EnsureEmailNotUsedAsync(email, id, ct);

        var match = await FindAccountAsync(email, ct);

        if (match != null &&
            profile.UserId != null &&
            match.Id != profile.UserId)
        {
            throw new InvalidOperationException(
                "Email da thuoc ve tai khoan khac.");
        }

        // Lien ket ho so voi tai khoan neu phu hop
        if (match != null && profile.UserId == null)
        {
            bool alreadyLinked =
                await _db.EmployeeProfiles.AnyAsync(
                    p => p.UserId == match.Id &&
                         p.Id != id,
                    ct
                );

            if (alreadyLinked)
            {
                throw new InvalidOperationException(
                    "Tai khoan nhan vien da co ho so khac.");
            }

            profile.UserId = match.Id;
            profile.User = match;
        }

        // Cap nhat du lieu ho so
        Apply(profile, request, email);

        profile.UpdatedAt = DateTime.UtcNow;

        // Dong bo voi tai khoan dang nhap neu da lien ket
        if (profile.User != null)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException(
                    "Ho so da lien ket tai khoan phai co Email.");
            }

            bool duplicateUserEmail =
                await _db.Users.AnyAsync(
                    u => u.Email == email &&
                         u.Id != profile.User.Id,
                    ct
                );

            if (duplicateUserEmail)
            {
                throw new InvalidOperationException(
                    "Email da duoc su dung boi tai khoan khac.");
            }

            profile.User.Email = email;
            SyncAccount(profile.User, profile);
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return (await GetByIdAsync(profile.Id, ct))!;
    }

    // =====================================================
    // 5. Kiem tra email da ton tai trong ho so
    // =====================================================
    private async Task EnsureEmailNotUsedAsync(
        string? email,
        Guid? excludedId,
        CancellationToken ct)
    {
        if (email == null)
            return;

        bool exists = await _db.EmployeeProfiles
            .AnyAsync(p =>
                p.Email == email &&
                (!excludedId.HasValue ||
                 p.Id != excludedId.Value),
                ct
            );

        if (exists)
        {
            throw new InvalidOperationException(
                "Email da duoc su dung trong ho so khac.");
        }
    }

    // =====================================================
    // 6. Tim tai khoan Employee theo email
    // =====================================================
    private async Task<User?> FindAccountAsync(
        string? email,
        CancellationToken ct)
    {
        if (email == null)
            return null;

        var user = await _db.Users
            .FirstOrDefaultAsync(
                u => u.Email == email, ct);

        if (user != null &&
            user.Role != UserRole.Employee)
        {
            throw new InvalidOperationException(
                "Email thuoc tai khoan khong phai nhan vien.");
        }

        return user;
    }

    // =====================================================
    // 7. Kiem tra du lieu bat buoc
    // =====================================================
    private static void Validate(
        EmployeeProfileRequest request)
    {
        // Ho va ten
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            request.FullName.Trim().Length < 2 ||
            request.FullName.Trim().Length > 200)
        {
            throw new ArgumentException(
                "Ho ten phai co tu 2 den 200 ky tu.");
        }

        // So dien thoai Viet Nam
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                request.PhoneNumber.Trim(),
                @"^0[0-9]{9}$"))
        {
            throw new ArgumentException(
                "So dien thoai phai gom 10 chu so va bat dau bang 0.");
        }

        // Ngay sinh
        if (request.DateOfBirth == default ||
            request.DateOfBirth >=
                DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException(
                "Ngay sinh phai la mot ngay trong qua khu.");
        }

        // Chuc vu
        if (string.IsNullOrWhiteSpace(request.Position) ||
            request.Position.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Vui long nhap chuc vu hop le.");
        }

        // Phong ban
        if (string.IsNullOrWhiteSpace(request.Department) ||
            request.Department.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Vui long nhap phong ban hop le.");
        }

        // Email
        var email = NormalizeEmail(request.Email);

        if (email != null &&
            !new EmailAddressAttribute().IsValid(email))
        {
            throw new ArgumentException(
                "Email khong dung dinh dang.");
        }
    }

    // =====================================================
    // 8. Chuan hoa email
    // =====================================================
    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();
    }

    // =====================================================
    // 9. Tu dong tao ma nhan vien
    // =====================================================
    private static string MakeCode(Guid id)
    {
        return "NV" +
            id.ToString("N")[..8].ToUpperInvariant();
    }

    // =====================================================
    // 10. Gan du lieu tu Request vao EmployeeProfile
    // =====================================================
    private static void Apply(
        EmployeeProfile profile,
        EmployeeProfileRequest request,
        string? email)
    {
        profile.FullName = request.FullName.Trim();
        profile.PhoneNumber = request.PhoneNumber.Trim();
        profile.DateOfBirth = request.DateOfBirth;
        profile.Position = request.Position.Trim();
        profile.Department = request.Department.Trim();

        profile.Email = email;

        profile.Address =
            string.IsNullOrWhiteSpace(request.Address)
                ? null
                : request.Address.Trim();

        profile.JoinedOn = request.JoinedOn;

        profile.Notes =
            string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim();
    }

    // =====================================================
    // 11. Dong bo thong tin voi User
    // =====================================================
    private static void SyncAccount(
        User user,
        EmployeeProfile profile)
    {
        user.FullName = profile.FullName;
        user.PhoneNumber = profile.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;
    }

    // =====================================================
    // 12. Chuyen EmployeeProfile thanh Response
    // =====================================================
    private static EmployeeProfileResponse ToResponse(
        EmployeeProfile profile)
    {
        return new EmployeeProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            EmployeeCode = profile.EmployeeCode,
            FullName = profile.FullName,
            PhoneNumber = profile.PhoneNumber,
            DateOfBirth = profile.DateOfBirth,
            Position = profile.Position,
            Department = profile.Department,
            Email = profile.Email,
            Address = profile.Address,
            JoinedOn = profile.JoinedOn,
            Notes = profile.Notes,

            Status = (
                profile.User?.Status ??
                profile.Status
            ).ToString(),

            IsLinkedToAccount = profile.UserId.HasValue,
            IsProfileComplete = true
        };
    }

    // =====================================================
    // 13. Chuyen User cu thanh Response
    // =====================================================
    private static EmployeeProfileResponse FromUser(
        User user)
    {
        return new EmployeeProfileResponse
        {
            Id = user.Id,
            UserId = user.Id,
            EmployeeCode = MakeCode(user.Id),
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Email = user.Email,
            Status = user.Status.ToString(),
            IsLinkedToAccount = true,
            IsProfileComplete = false
        };
    }
}
