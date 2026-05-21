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
        return "bg-amber-50 text-amber-700 ring-1 ring-amber-200";
      case TaskStatus.InProgress:
        return "bg-blue-50 text-blue-700 ring-1 ring-blue-200";
      case TaskStatus.Done:
        return "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200";
      default:
        return "bg-slate-100 text-slate-600 ring-1 ring-slate-200";
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
    <div className="bg-white rounded-xl border border-slate-200 p-5 hover:shadow-md hover:-translate-y-0.5 transition-all duration-200">
      <div className="flex justify-between items-start gap-3 mb-3">
        <h3 className="text-base font-semibold text-slate-900 leading-snug flex-1">
          {task.Title}
        </h3>
        <span
          className={`shrink-0 px-2.5 py-0.5 rounded-full text-xs font-semibold ${getStatusColor(task.Status)}`}
        >
          {getStatusText(task.Status)}
        </span>
      </div>

      <p className="text-sm text-slate-500 mb-4 line-clamp-2 leading-relaxed">{task.Description}</p>

      <div className="space-y-1.5 text-xs text-slate-500 mb-4">
        <div className="flex items-center gap-1.5">
          <Calendar className="h-3.5 w-3.5 text-blue-500 shrink-0" />
          <span>Vence: {formatDate(task.DueDate, "PPP")}</span>
        </div>

        {task.AssignedToName && (
          <div className="flex items-center gap-1.5">
            <User className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
            <span>Asignado a: {task.AssignedToName}</span>
          </div>
        )}

        <div className="flex items-center gap-1.5">
          <Clock className="h-3.5 w-3.5 text-slate-400 shrink-0" />
          <span>Creado: {formatDate(task.CreatedAt, "PPp")}</span>
        </div>
      </div>

      <div className="flex gap-2 pt-3 border-t border-slate-100">
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
