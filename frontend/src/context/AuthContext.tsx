import { createContext, useContext, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";

interface User {
  id: number;
  email: string;
}

interface AuthResponse {
  authenticated: boolean;
  user?: User;
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (userData: User) => void;
  logout: () => void;
  refetchAuth: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();

  const { data, isLoading, refetch } = useQuery<AuthResponse>({
    queryKey: ["auth"],
    queryFn: async () => {
      const response = await fetch("http://localhost:5203/api/authorized", {
        credentials: "include",
      });

      if (!response.ok) {
        return { authenticated: false };
      }

      return response.json();
    },
    staleTime: 5 * 60 * 1000,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  const user = data?.authenticated && data.user ? data.user : null;
  const isAuthenticated = data?.authenticated === true;

  const login = (userData: User) => {
    queryClient.setQueryData(["auth"], {
      authenticated: true,
      user: userData,
    });
  };

  const logout = async () => {
    try {
      await fetch("http://localhost:5203/api/logout", {
        method: "POST",
        credentials: "include",
      });
    } catch (error) {
      console.error("Logout failed:", error);
    }

    queryClient.setQueryData(["auth"], {
      authenticated: false,
    });
  };

  const refetchAuth = () => {
    refetch();
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
        isLoading,
        login,
        logout,
        refetchAuth,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
