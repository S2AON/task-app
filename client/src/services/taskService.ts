import api from "./api";
import {
  Task,
  CreateTaskRequest,
  UpdateTaskRequest,
  TaskStatus,
} from "@/types";

export interface TaskFilters {
  search?: string;
  status?: TaskStatus;
  assignedTo?: string;
  dueDateFrom?: string;
  dueDateTo?: string;
  sortBy?: "title" | "duedate" | "status" | "createdat";
  sortDescending?: boolean;
}

export const taskService = {
  async getTasks(filters?: TaskFilters): Promise<Task[]> {
    const params = new URLSearchParams();

    if (filters?.search) params.append("search", filters.search);
    if (filters?.status !== undefined)
      params.append("status", filters.status.toString());
    if (filters?.assignedTo) params.append("assignedTo", filters.assignedTo);
    if (filters?.dueDateFrom) params.append("dueDateFrom", filters.dueDateFrom);
    if (filters?.dueDateTo) params.append("dueDateTo", filters.dueDateTo);
    if (filters?.sortBy) params.append("sortBy", filters.sortBy);
    if (filters?.sortDescending !== undefined)
      params.append("sortDescending", filters.sortDescending.toString());

    const url = params.toString() ? `/tasks?${params.toString()}` : "/tasks";
    const response = await api.get(url);
    return response.data;
  },

  async getTaskById(id: string): Promise<Task> {
    const response = await api.get(`/tasks/${id}`);
    return response.data;
  },

  async createTask(data: CreateTaskRequest): Promise<Task> {
    const response = await api.post("/tasks", data);
    return response.data;
  },

  async updateTask(id: string, data: UpdateTaskRequest): Promise<Task> {
    const response = await api.put(`/tasks/${id}`, data);
    return response.data;
  },

  async deleteTask(id: string): Promise<void> {
    await api.delete(`/tasks/${id}`);
  },
};
