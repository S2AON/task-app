import api from "./api";
import { User, LoginRequest, RegisterRequest } from "@/types";

type AuthResponse = {
  token: string;
  email: string;
  fullName: string;
  userId: string;
};

function toUser(data: AuthResponse): User {
  return {
    token: data.token,
    email: data.email,
    fullName: data.fullName,
    userId: data.userId,
  };
}

function persist(user: User): void {
  localStorage.setItem("token", user.token);
  localStorage.setItem("user", JSON.stringify(user));
}

export const authService = {
  async login(credentials: LoginRequest): Promise<User> {
    const { data } = await api.post<AuthResponse>("/auth/login", credentials);
    const user = toUser(data);
    persist(user);
    return user;
  },

  async register(userData: RegisterRequest): Promise<User> {
    const { data } = await api.post<AuthResponse>("/auth/register", userData);
    const user = toUser(data);
    persist(user);
    return user;
  },

  async googleLogin(idToken: string): Promise<User> {
    const { data } = await api.post<AuthResponse>("/auth/google", { idToken });
    const user = toUser(data);
    persist(user);
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
