import React from "react";
import { Search, Filter } from "lucide-react";
import { TaskStatus } from "@/types";
import { Input } from "@components/common/Input";

interface TaskFiltersProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  statusFilter: TaskStatus | "all";
  onStatusChange: (value: TaskStatus | "all") => void;
}

export const TaskFilters: React.FC<TaskFiltersProps> = ({
  searchTerm,
  onSearchChange,
  statusFilter,
  onStatusChange,
}) => {
  return (
    <div className="bg-white rounded-xl border border-slate-200 p-4 mb-6">
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex-1">
          <Input
            type="text"
            placeholder="Buscar tareas..."
            value={searchTerm}
            onChange={(e) => onSearchChange(e.target.value)}
            icon={Search}
          />
        </div>

        <div className="sm:w-56">
          <div className="relative">
            <Filter className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400 pointer-events-none" />
            <select
              value={statusFilter}
              onChange={(e) => {
                const val = e.target.value;
                onStatusChange(val === "all" ? "all" : (Number(val) as TaskStatus));
              }}
              className="block w-full pl-10 pr-4 py-2.5 text-sm rounded-lg border border-slate-300 bg-white text-slate-900 outline-none transition-all duration-150 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 appearance-none"
            >
              <option value="all">Todos los estados</option>
              <option value={TaskStatus.Pending}>Pendiente</option>
              <option value={TaskStatus.InProgress}>En Progreso</option>
              <option value={TaskStatus.Done}>Completada</option>
            </select>
          </div>
        </div>
      </div>
    </div>
  );
};
