import { Task, TaskStatus } from "@/types";
import { Calendar, User, Clock, Edit, Trash2 } from "lucide-react";
import { format, isValid, parseISO } from "date-fns";
import { es } from "date-fns/locale";
import { Button } from "@components/common/Button";

interface TaskCardProps {
  task: Task;
  onEdit: (task: Task) => void;
  onDelete: (id: string) => void;
}

export const TaskCard: React.FC<TaskCardProps> = ({
  task,
  onEdit,
  onDelete,
}) => {
  const getStatusColor = (status: TaskStatus) => {
    switch (status) {
      case TaskStatus.Pending:
        return "bg-yellow-100 text-yellow-800 border-yellow-300";
      case TaskStatus.InProgress:
        return "bg-blue-100 text-blue-800 border-blue-300";
      case TaskStatus.Done:
        return "bg-green-100 text-green-800 border-green-300";
      default:
        return "bg-gray-100 text-gray-800 border-gray-300";
    }
  };

  const getStatusText = (status: TaskStatus) => {
    switch (status) {
      case TaskStatus.Pending:
        return "Pendiente";
      case TaskStatus.InProgress:
        return "En Progreso";
      case TaskStatus.Done:
        return "Completada";
      default:
        return "Desconocido";
    }
  };
  const formatDate = (dateString: string, formatStr: string) => {
    try {
      const date =
        typeof dateString === "string"
          ? parseISO(dateString)
          : new Date(dateString);
      if (!isValid(date)) {
        console.warn("Fecha inválida:", dateString);
        return "Fecha inválida";
      }
      return format(date, formatStr, { locale: es });
    } catch (error) {
      console.error("Error formateando fecha:", dateString, error);
      return "Fecha inválida";
    }
  };

  return (
    <div className="bg-white rounded-lg shadow-md p-6 hover:shadow-xl transition-all border border-gray-200">
      <div className="flex justify-between items-start mb-4">
        <h3 className="text-xl font-semibold text-gray-900 flex-1">
          {task.Title}
        </h3>
        <span
          className={`px-3 py-1 rounded-full text-xs font-semibold border ${getStatusColor(
            task.Status
          )}`}
        >
          {getStatusText(task.Status)}
        </span>
      </div>

      <p className="text-gray-600 mb-4 line-clamp-2">{task.Description}</p>

      <div className="space-y-2 text-sm text-gray-500 mb-4">
        <div className="flex items-center space-x-2">
          <Calendar className="h-4 w-4 text-blue-500" />
          <span>Vence: {formatDate(task.DueDate, "PPP")}</span>
        </div>

        {task.AssignedToName && (
          <div className="flex items-center space-x-2">
            <User className="h-4 w-4 text-green-500" />
            <span>Asignado a: {task.AssignedToName}</span>
          </div>
        )}

        <div className="flex items-center space-x-2">
          <Clock className="h-4 w-4 text-gray-400" />
          <span>Creado: {formatDate(task.CreatedAt, "PPp")}</span>
        </div>
      </div>

      <div className="flex space-x-2 pt-4 border-t">
        <Button
          variant="primary"
          icon={Edit}
          onClick={() => onEdit(task)}
          className="flex-1"
        >
          Editar
        </Button>
        <Button
          variant="danger"
          icon={Trash2}
          onClick={() => onDelete(task.Id)}
          className="flex-1"
        >
          Eliminar
        </Button>
      </div>
    </div>
  );
};
