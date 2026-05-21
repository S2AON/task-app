import api from "./api";
import { User, LoginRequest, RegisterRequest } from "@/types";

export const authService = {
  async login(credentials: LoginRequest): Promise<User> {
    const response = await api.post("/auth/login", credentials);
    const data = response.data;

    if (!data.token && !data.Token) {
      throw new Error("Token no recibido del servidor");
    }

    const user: User = {
      token: data.token || data.Token,
      email: data.email || data.Email,
      fullName: data.fullName || data.FullName,
      userId: data.userId || data.UserId,
    };

    // Guardar token
    localStorage.setItem("token", user.token);
    // Guardar user
    localStorage.setItem("user", JSON.stringify(user));

    return user;
  },

  async register(userData: RegisterRequest): Promise<User> {
    const response = await api.post("/auth/register", userData);
    const data = response.data;

    const user: User = {
      token: data.token || data.Token,
      email: data.email || data.Email,
      fullName: data.fullName || data.FullName,
      userId: data.userId || data.UserId,
    };

    localStorage.setItem("token", user.token);
    localStorage.setItem("user", JSON.stringify(user));
    return user;
  },

  logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
  },

  getCurrentUser(): User | null {
    const userStr = localStorage.getItem("user");
    const token = localStorage.getItem("token");

    if (userStr && token) {
      try {
        const user = JSON.parse(userStr);
        if (!user.token) {
          user.token = token;
        }
        return user;
      } catch (error) {
        return null;
      }
    }

    return null;
  },

  isAuthenticated(): boolean {
    return !!localStorage.getItem("token") && !!localStorage.getItem("user");
  },
};
