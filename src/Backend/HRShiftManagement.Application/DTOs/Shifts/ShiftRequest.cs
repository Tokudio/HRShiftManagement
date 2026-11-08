using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.ComponentModel.DataAnnotations;

namespace HRShiftManagement.Application.DTOs.Shifts;

public class ShiftRequest
{
    [Required(ErrorMessage = "Tên ca không được để trống.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public TimeOnly? StartTime { get; set; }

    [Required]
    public TimeOnly? EndTime { get; set; }

    [Range(1, 10000)]
    public int MinEmployees { get; set; }

    [Range(1, 10000)]
    public int MaxEmployees { get; set; }
}

