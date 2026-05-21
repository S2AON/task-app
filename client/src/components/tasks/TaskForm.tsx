import React, { useState, useEffect } from "react";
import {
  Task,
  CreateTaskRequest,
  UpdateTaskRequest,
  TaskStatus,
} from "@/types";
import { Input } from "@components/common/Input";
import { Button } from "@components/common/Button";
import { Save, X } from "lucide-react";

interface TaskFormProps {
  task?: Task;
  onSubmit: (data: CreateTaskRequest | UpdateTaskRequest) => Promise<void>;
  onCancel: () => void;
}

export const TaskForm: React.FC<TaskFormProps> = ({
  task,
  onSubmit,
  onCancel,
}) => {
  const [formData, setFormData] = useState({
    title: "",
    description: "",
    dueDate: "",
    status: TaskStatus.Pending,
  });
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (task) {
      setFormData({
        title: task.Title,
        description: task.Description,
        dueDate: new Date(task.DueDate).toISOString().split("T")[0],
        status: task.Status,
      });
    }
  }, [task]);

  const validate = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.title.trim()) {
      newErrors.title = "El título es requerido";
    }

    if (!formData.description.trim()) {
      newErrors.description = "La descripción es requerida";
    }

    if (!formData.dueDate) {
      newErrors.dueDate = "La fecha de vencimiento es requerida";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validate()) return;

    setLoading(true);
    try {
      const submitData = task
        ? {
            title: formData.title,
            description: formData.description,
            dueDate: new Date(formData.dueDate).toISOString(),
            status: formData.status,
            assignedTo: null,
          }
        : {
            title: formData.title,
            description: formData.description,
            dueDate: new Date(formData.dueDate).toISOString(),
            assignedTo: null,
          };

      await onSubmit(submitData);
    } catch (error) {
      console.error("Error submitting form:", error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <Input
        label="Título"
        type="text"
        value={formData.title}
        onChange={(e) => setFormData({ ...formData, title: e.target.value })}
        error={errors.title}
        placeholder="Ej: Revisar documentación"
      />

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Descripción
        </label>
        <textarea
          value={formData.description}
          onChange={(e) =>
            setFormData({ ...formData, description: e.target.value })
          }
          rows={4}
          className={`
            block w-full rounded-lg border border-gray-300 px-4 py-2
            focus:ring-2 focus:ring-blue-500 focus:border-transparent
            ${errors.description ? "border-red-500" : ""}
          `}
          placeholder="Describe la tarea en detalle..."
        />
        {errors.description && (
          <p className="mt-1 text-sm text-red-600">{errors.description}</p>
        )}
      </div>

      <Input
        label="Fecha de Vencimiento"
        type="date"
        value={formData.dueDate}
        onChange={(e) => setFormData({ ...formData, dueDate: e.target.value })}
        error={errors.dueDate}
        min={new Date().toISOString().split("T")[0]}
      />

      {task && (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Estado
          </label>
          <select
            value={formData.status}
            onChange={(e) =>
              setFormData({
                ...formData,
                status: parseInt(e.target.value) as TaskStatus,
              })
            }
            className="block w-full rounded-lg border border-gray-300 px-4 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value={TaskStatus.Pending}>Pendiente</option>
            <option value={TaskStatus.InProgress}>En Progreso</option>
            <option value={TaskStatus.Done}>Completada</option>
          </select>
        </div>
      )}

      <div className="flex space-x-3 pt-4">
        <Button
          type="submit"
          variant="primary"
          icon={Save}
          loading={loading}
          className="flex-1"
        >
          {task ? "Actualizar" : "Crear"} Tarea
        </Button>
        <Button
          type="button"
          variant="secondary"
          icon={X}
          onClick={onCancel}
          className="flex-1"
        >
          Cancelar
        </Button>
      </div>
    </form>
  );
};
