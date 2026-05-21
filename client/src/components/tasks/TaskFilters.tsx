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
    <div className="bg-white rounded-lg shadow-md p-4 mb-6">
      <div className="flex flex-col md:flex-row gap-4">
        <div className="flex-1">
          <Input
            type="text"
            placeholder="Buscar tareas..."
            value={searchTerm}
            onChange={(e) => onSearchChange(e.target.value)}
            icon={Search}
          />
        </div>

        <div className="md:w-64">
          <div className="relative">
            <Filter className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" />
            <select
              value={statusFilter}
              onChange={(e) =>
                onStatusChange(e.target.value as TaskStatus | "all")
              }
              className="block w-full pl-10 pr-4 py-2 rounded-lg border border-gray-300 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
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
