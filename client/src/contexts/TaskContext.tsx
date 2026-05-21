import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from "react";
import { Task, CreateTaskRequest, UpdateTaskRequest } from "@/types";
import { taskService } from "@services/taskService";
import { useAuth } from "./AuthContext";

const POLL_INTERVAL_MS = 30_000;

interface TaskContextType {
  tasks: Task[];
  loading: boolean;
  error: string;
  createTask: (data: CreateTaskRequest) => Promise<void>;
  updateTask: (id: string, data: UpdateTaskRequest) => Promise<void>;
  deleteTask: (id: string) => Promise<void>;
  refresh: () => Promise<void>;
}

const TaskContext = createContext<TaskContextType | undefined>(undefined);

export const TaskProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const { isAuthenticated } = useAuth();
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchTasks = useCallback(async (showLoading = false) => {
    try {
      if (showLoading) setLoading(true);
      setError("");
      const data = await taskService.getTasks();
      setTasks(data);
    } catch (err: any) {
      setError(err.response?.data?.error || "Error al cargar las tareas");
    } finally {
      if (showLoading) setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!isAuthenticated) {
      setTasks([]);
      if (intervalRef.current) clearInterval(intervalRef.current);
      return;
    }

    fetchTasks(true);

    intervalRef.current = setInterval(() => fetchTasks(false), POLL_INTERVAL_MS);

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [isAuthenticated, fetchTasks]);

  const createTask = async (data: CreateTaskRequest) => {
    await taskService.createTask(data);
    await fetchTasks(false);
  };

  const updateTask = async (id: string, data: UpdateTaskRequest) => {
    await taskService.updateTask(id, data);
    await fetchTasks(false);
  };

  const deleteTask = async (id: string) => {
    await taskService.deleteTask(id);
    await fetchTasks(false);
  };

  return (
    <TaskContext.Provider
      value={{
        tasks,
        loading,
        error,
        createTask,
        updateTask,
        deleteTask,
        refresh: () => fetchTasks(false),
      }}
    >
      {children}
    </TaskContext.Provider>
  );
};

export const useTask = () => {
  const context = useContext(TaskContext);
  if (!context) throw new Error("useTask must be used within a TaskProvider");
  return context;
};
