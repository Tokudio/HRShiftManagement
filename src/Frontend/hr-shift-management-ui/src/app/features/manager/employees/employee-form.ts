import {
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';

import { FormsModule } from '@angular/forms';

import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import {
  EmployeeProfile,
  EmployeeProfileRequest,
  EmployeeProfileService
} from '../../../core/services/employee-profile.service';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './employee-form.html',
  styleUrl: './employee-form.scss'
})
export class EmployeeForm implements OnInit {

  // ============================================
  // 1. INJECT SERVICES
  // ============================================

  private readonly employeeService =
    inject(EmployeeProfileService);

  private readonly route =
    inject(ActivatedRoute);

  private readonly router =
    inject(Router);

  private readonly cdr =
    inject(ChangeDetectorRef);

  // ============================================
  // 2. THONG TIN NHAN VIEN
  // ============================================

  employeeId: string | null = null;

  employeeCode = 'Tự động tạo';

  linkedAccount = false;

  // ============================================
  // 3. TRANG THAI GIAO DIEN
  // ============================================

  loading = false;
  saving = false;

  errorMessage = '';
  successMessage = '';

  today = this.localDate(new Date());

  // ============================================
  // 4. DANH SACH PHONG BAN
  // ============================================

  departments: string[] = [
    'Bán hàng',
    'Kinh doanh',
    'Nhân sự',
    'Kho',
    'Vận hành',
    'Văn phòng',
    'Kỹ thuật',
    'Khác'
  ];

  // ============================================
  // 5. DU LIEU FORM
  // ============================================

  form: EmployeeProfileRequest = {
    fullName: '',
    phoneNumber: '',
    dateOfBirth: '',
    position: '',
    department: '',
    email: null,
    address: null,
    joinedOn: null,
    notes: null
  };

  // ============================================
  // 6. KHOI TAO COMPONENT
  // ============================================

  ngOnInit(): void {

    const id = this.route.snapshot.paramMap.get('id');

    // Khong co ID => Trang them nhan vien
    if (!id) {
      return;
    }

    // Co ID => Trang chinh sua nhan vien
    this.employeeId = id;

    this.loadEmployee(id);
  }

  // ============================================
  // 7. LAY THONG TIN NHAN VIEN
  // ============================================

  loadEmployee(id: string): void {

    this.loading = true;
    this.errorMessage = '';

    this.employeeService.getById(id).subscribe({

      next: (employee: EmployeeProfile) => {

        this.populateForm(employee);

        this.loading = false;

        this.cdr.markForCheck();
      },

      error: (err) => {

        console.error(
          'Lỗi tải hồ sơ nhân viên:',
          err
        );

        this.errorMessage =
          err.error?.message ||
          'Không tìm thấy hồ sơ nhân viên.';

        this.loading = false;

        this.cdr.markForCheck();
      }
    });
  }

  // ============================================
  // 8. CHUYEN NGAY THANH YYYY-MM-DD
  // ============================================

  private localDate(date: Date): string {

    const year = date.getFullYear();

    const month = String(
      date.getMonth() + 1
    ).padStart(2, '0');

    const day = String(
      date.getDate()
    ).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  // ============================================
  // 9. DO DU LIEU VAO FORM
  // ============================================

  private populateForm(
    employee: EmployeeProfile
  ): void {

    this.employeeCode = employee.employeeCode;

    this.linkedAccount =
      employee.isLinkedToAccount;

    this.form = {

      fullName: employee.fullName || '',

      phoneNumber: employee.phoneNumber || '',

      dateOfBirth: employee.dateOfBirth || '',

      position: employee.position || '',

      department: employee.department || '',

      email: employee.email || null,

      address: employee.address || null,

      joinedOn: employee.joinedOn || null,

      notes: employee.notes || null
    };

    // Neu phong ban chua co trong danh sach
    // thi tu dong them vao
    if (
      employee.department &&
      !this.departments.includes(employee.department)
    ) {
      this.departments = [
        ...this.departments,
        employee.department
      ];
    }
  }

  // ============================================
  // 10. KIEM TRA DU LIEU FORM
  // ============================================

  private validateForm(
    request: EmployeeProfileRequest
  ): boolean {

    // Xoa thong bao loi cu
    this.errorMessage = '';

    // ==========================================
    // KIEM TRA TRUONG BAT BUOC
    // ==========================================

    if (
      !request.fullName ||
      !request.phoneNumber ||
      !request.dateOfBirth ||
      !request.position ||
      !request.department
    ) {

      this.errorMessage =
        'Vui lòng nhập đầy đủ họ tên, số điện thoại, ngày sinh, chức vụ và phòng ban.';

      return false;
    }

    // ==========================================
    // KIEM TRA HO TEN
    // ==========================================

    if (
      request.fullName.length < 2 ||
      request.fullName.length > 200
    ) {

      this.errorMessage =
        'Họ tên phải có từ 2 đến 200 ký tự.';

      return false;
    }

    // ==========================================
    // KIEM TRA SO DIEN THOAI
    // ==========================================

    const phoneRegex = /^0[0-9]{9}$/;

    if (!phoneRegex.test(request.phoneNumber)) {

      this.errorMessage =
        'Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0.';

      return false;
    }

    // ==========================================
    // KIEM TRA EMAIL
    // ==========================================

    if (request.email) {

      const emailRegex =
        /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

      if (!emailRegex.test(request.email)) {

        this.errorMessage =
          'Email không đúng định dạng. Ví dụ: abc@gmail.com';

        return false;
      }
    }

    // ==========================================
    // KIEM TRA NGAY SINH
    // ==========================================

    const birthDate = new Date(
      `${request.dateOfBirth}T00:00:00`
    );

    if (
      Number.isNaN(birthDate.getTime()) ||
      this.localDate(birthDate) !== request.dateOfBirth ||
      request.dateOfBirth >= this.today
    ) {

      this.errorMessage =
        'Ngày sinh không hợp lệ hoặc không nằm trong quá khứ.';

      return false;
    }

    // ==========================================
    // KIEM TRA CHUC VU
    // ==========================================

    if (request.position.length > 100) {

      this.errorMessage =
        'Chức vụ không được vượt quá 100 ký tự.';

      return false;
    }

    // ==========================================
    // KIEM TRA PHONG BAN
    // ==========================================

    if (request.department.length > 100) {

      this.errorMessage =
        'Phòng ban không được vượt quá 100 ký tự.';

      return false;
    }

    // ==========================================
    // KIEM TRA NGAY VAO LAM
    // ==========================================

    if (request.joinedOn) {

      const joinedDate = new Date(
        `${request.joinedOn}T00:00:00`
      );

      if (
        Number.isNaN(joinedDate.getTime()) ||
        this.localDate(joinedDate) !== request.joinedOn ||
        request.joinedOn > this.today
      ) {

        this.errorMessage =
          'Ngày vào làm không hợp lệ hoặc lớn hơn ngày hiện tại.';

        return false;
      }
    }

    // ==========================================
    // KIEM TRA EMAIL TAI KHOAN LIEN KET
    // ==========================================

    if (
      this.linkedAccount &&
      !request.email
    ) {

      this.errorMessage =
        'Hồ sơ đã liên kết tài khoản phải có Email.';

      return false;
    }

    // Tat ca du lieu hop le
    return true;
  }

  // ============================================
  // 11. THEM HOAC CAP NHAT HO SO
  // ============================================

  save(): void {

    if (this.saving || this.loading) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';

    // Chuan hoa du lieu truoc khi gui
    const request: EmployeeProfileRequest = {

      fullName:
        this.form.fullName.trim(),

      phoneNumber:
        this.form.phoneNumber.trim(),

      dateOfBirth:
        this.form.dateOfBirth,

      position:
        this.form.position.trim(),

      department:
        this.form.department.trim(),

      email:
        this.form.email?.trim() || null,

      address:
        this.form.address?.trim() || null,

      joinedOn:
        this.form.joinedOn || null,

      notes:
        this.form.notes?.trim() || null
    };

    // Kiem tra du lieu
    if (!this.validateForm(request)) {
      return;
    }

    // Bat dau luu
    this.saving = true;

    // US 1.5 - Tao ho so
    // US 1.6 - Cap nhat ho so
    const action = this.employeeId
      ? this.employeeService.update(
          this.employeeId,
          request
        )
      : this.employeeService.create(request);

    action.subscribe({

      // ========================================
      // LUU THANH CONG
      // ========================================

      next: (employee: EmployeeProfile) => {

        this.saving = false;

        if (!this.employeeId) {

          // US 1.5 - THEM NHAN VIEN THANH CONG

          this.router.navigate(
            ['/manager/employees'],
            {
              queryParams: {
                created: '1'
              }
            }
          );

        } else {

          // US 1.6 - CAP NHAT THANH CONG

          this.populateForm(employee);

          this.successMessage =
            'Cập nhật hồ sơ nhân viên thành công.';

          this.errorMessage = '';

          this.cdr.markForCheck();
        }
      },

      // ========================================
      // LUU THAT BAI
      // ========================================

      error: (err) => {

        console.error(
          'Lỗi lưu hồ sơ nhân viên:',
          err
        );

        this.saving = false;

        const validationErrors =
          err.error?.errors;

        if (validationErrors) {

          const messages = Object.values(
            validationErrors
          )
            .flat()
            .map(value => String(value));

          this.errorMessage =
            messages.join(' ') ||
            'Dữ liệu không hợp lệ.';

        } else {

          this.errorMessage =
            err.error?.message ||
            err.error?.title ||
            'Không thể lưu hồ sơ nhân viên.';
        }

        this.cdr.markForCheck();
      }
    });
  }
}
