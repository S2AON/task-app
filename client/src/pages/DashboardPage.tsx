import React, { useState, useEffect } from "react";
import { useTask } from "@/contexts/TaskContext";
import { Task, TaskStatus, CreateTaskRequest, UpdateTaskRequest } from "@/types";
import { Layout } from "@components/layout/Layout";
import { TaskCard } from "@components/tasks/TaskCard";
import { TaskForm } from "@components/tasks/TaskForm";
import { TaskFilters } from "@components/tasks/TaskFilters";
import { Modal } from "@components/common/Modal";
import { Button } from "@components/common/Button";
import { Spinner } from "@components/common/Spinner";
import { Plus, AlertCircle } from "lucide-react";

export const Dashboard: React.FC = () => {
  const { tasks, loading, error, createTask, updateTask, deleteTask } = useTask();

  const [filteredTasks, setFilteredTasks] = useState<Task[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | undefined>(undefined);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<TaskStatus | "all">("all");

  useEffect(() => {
    let filtered = tasks;

    if (searchTerm.trim()) {
      const searchLower = searchTerm.toLowerCase();
      filtered = filtered.filter(
        (task) =>
          task.Title.toLowerCase().includes(searchLower) ||
          task.Description.toLowerCase().includes(searchLower)
      );
    }

    if (statusFilter !== "all") {
      filtered = filtered.filter((task) => task.Status === statusFilter);
    }

    setFilteredTasks(filtered);
  }, [tasks, searchTerm, statusFilter]);

  const handleSubmitTask = async (data: CreateTaskRequest | UpdateTaskRequest) => {
    try {
      if (editingTask) {
        await updateTask(editingTask.Id, data as UpdateTaskRequest);
      } else {
        await createTask(data as CreateTaskRequest);
      }
      setIsModalOpen(false);
      setEditingTask(undefined);
    } catch (err: any) {
      alert(err.response?.data?.error || "Error al procesar la tarea");
    }
  };

  const handleDeleteTask = async (id: string) => {
    if (!confirm("¿Estás seguro de eliminar esta tarea?")) return;

    try {
      await deleteTask(id);
    } catch (err: any) {
      alert(err.response?.data?.error || "Error al eliminar la tarea");
    }
  };

  const handleEditTask = (task: Task) => {
    setEditingTask(task);
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setEditingTask(undefined);
  };

  const handleNewTask = () => {
    setEditingTask(undefined);
    setIsModalOpen(true);
  };

  if (loading) {
    return (
      <Layout>
        <Spinner fullScreen />
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="space-y-6">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Mis Tareas</h1>
            <p className="text-gray-600 mt-1">
              {filteredTasks.length}{" "}
              {filteredTasks.length === 1 ? "tarea" : "tareas"}
              {searchTerm || statusFilter !== "all" ? " encontradas" : " en total"}
            </p>
          </div>
          <Button variant="primary" icon={Plus} onClick={handleNewTask}>
            Nueva Tarea
          </Button>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg flex items-center space-x-2">
            <AlertCircle className="h-5 w-5" />
            <span>{error}</span>
          </div>
        )}

        <TaskFilters
          searchTerm={searchTerm}
          onSearchChange={setSearchTerm}
          statusFilter={statusFilter}
          onStatusChange={setStatusFilter}
        />

        {filteredTasks.length === 0 ? (
          <div className="text-center py-12">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-gray-100 rounded-full mb-4">
              <AlertCircle className="h-8 w-8 text-gray-400" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">No hay tareas</h3>
            <p className="text-gray-600 mb-4">
              {searchTerm || statusFilter !== "all"
                ? "No se encontraron tareas con los filtros aplicados"
                : "Comienza creando tu primera tarea"}
            </p>
            {!searchTerm && statusFilter === "all" && (
              <Button variant="primary" icon={Plus} onClick={handleNewTask}>
                Crear Primera Tarea
              </Button>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {filteredTasks.map((task) => (
              <TaskCard
                key={task.Id}
                task={task}
                onEdit={handleEditTask}
                onDelete={handleDeleteTask}
              />
            ))}
          </div>
        )}

        <Modal
          isOpen={isModalOpen}
          onClose={handleCloseModal}
          title={editingTask ? "Editar Tarea" : "Nueva Tarea"}
        >
          <TaskForm
            task={editingTask}
            onSubmit={handleSubmitTask}
            onCancel={handleCloseModal}
          />
        </Modal>
      </div>
    </Layout>
  );
};
