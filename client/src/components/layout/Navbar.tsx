import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/contexts/AuthContext";
import { LogOut, CheckSquare, User } from "lucide-react";
import { Button } from "@components/common/Button";

export const Navbar: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <nav className="bg-white border-b border-slate-200 sticky top-0 z-40">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between h-16">
          <div className="flex items-center">
            <Link to="/" className="flex items-center gap-2 group">
              <CheckSquare className="h-7 w-7 text-blue-600 group-hover:text-blue-700 transition-colors" />
              <span className="text-lg font-bold text-slate-900">Task App</span>
            </Link>
          </div>

          <div className="flex items-center gap-3">
            {user && (
              <>
                <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 bg-slate-50 rounded-full border border-slate-200">
                  <div className="h-6 w-6 rounded-full bg-blue-600 flex items-center justify-center">
                    <User className="h-3.5 w-3.5 text-white" />
                  </div>
                  <span className="text-sm font-medium text-slate-700">{user.fullName}</span>
                </div>
                <Button variant="secondary" icon={LogOut} onClick={handleLogout}>
                  Salir
                </Button>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
};
