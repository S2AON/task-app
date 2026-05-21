export enum TaskStatus {
  Pending = 0,
  InProgress = 1,
  Done = 2,
}

export interface Task {
  Id: string;
  Title: string;
  Description: string;
  DueDate: string;
  Status: TaskStatus;
  CreatedBy: string;
  CreatorName: string;
  AssignedTo: string | null;
  AssignedToName: string | null;
  CreatedAt: string;
  UpdatedAt: string | null;
}

export interface User {
  userId: string;
  email: string;
  fullName: string;
  token: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  dueDate: string;
  assignedTo: string | null;
}

export interface UpdateTaskRequest {
  title: string;
  description: string;
  dueDate: string;
  status: TaskStatus;
  assignedTo: string | null;
}
