
export interface Employee {
  id: string;
  employeeCode: string;
  fullName: string;
  email: string | null;
  phoneNumber: string;
  dateOfBirth: string;
  position: string;
  department: string;
  status: string;
}

export interface EmployeeRequest {
  employeeCode: string;
  fullName: string;
  email: string | null;
  phoneNumber: string;
  dateOfBirth: string;
  position: string;
  department: string;
}
