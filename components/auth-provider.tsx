"use client"

import { createContext, useContext, useState, useEffect, ReactNode } from "react"
import { useRouter } from "next/navigation"
import { api } from "@/lib/api-client"

interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  phone?: string
  roles?: string[]
}

interface AuthContextType {
  user: User | null
  login: (email: string, password: string) => Promise<{ success: boolean; redirectTo?: string }>
  signup: (email: string, password: string, firstName: string, lastName: string) => Promise<boolean>
  logout: () => void
  isAuthenticated: boolean
  isInitialized: boolean
  userId: string | null
  isSuperAdmin: boolean
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isInitialized, setIsInitialized] = useState(false)
  const router = useRouter()

  useEffect(() => {
    // Check if user is logged in on mount
    const savedUser = localStorage.getItem("currentUser")
    const savedToken = localStorage.getItem("authToken")
    
    if (savedUser && savedToken) {
      try {
        const parsedUser = JSON.parse(savedUser)
        setUser(parsedUser)
        // Verify token is still valid by calling /api/auth/me
        api.auth.me()
          .then(() => {
            // Token is valid, keep user
            setIsInitialized(true)
          })
          .catch((error: any) => {
            // Token invalid (401/403) or other error, clear storage
            if (error?.status === 401 || error?.status === 403) {
              localStorage.removeItem("currentUser")
              localStorage.removeItem("authToken")
              setUser(null)
            }
            // Mark as initialized even if verification failed (could be network issue)
            setIsInitialized(true)
          })
      } catch (e) {
        localStorage.removeItem("currentUser")
        localStorage.removeItem("authToken")
        setIsInitialized(true)
      }
    } else {
      // No saved user/token, mark as initialized immediately
      setIsInitialized(true)
    }
  }, [])

  const login = async (email: string, password: string): Promise<{ success: boolean; redirectTo?: string }> => {
    try {
      const response = await api.auth.login(email, password) as {
        accessToken: string
        refreshToken?: string
        expiresAt?: string
        user: {
          id: number
          email: string
          fullName: string
          isActive?: boolean
          roles?: string[]
        }
      }

      if (response.accessToken && response.user) {
        // Store token and user data
        localStorage.setItem("authToken", response.accessToken)
        if (response.refreshToken) {
          localStorage.setItem("refreshToken", response.refreshToken)
        }
        
        // Parse fullName into firstName and lastName
        const nameParts = response.user.fullName?.split(" ") || []
        const firstName = nameParts[0] || ""
        const lastName = nameParts.slice(1).join(" ") || ""
        
        const userData: User = {
          id: response.user.id.toString(),
          email: response.user.email,
          firstName,
          lastName,
          roles: response.user.roles || [],
        }
        
        setUser(userData)
        localStorage.setItem("currentUser", JSON.stringify(userData))
        
        // Check if user is SuperAdmin and determine redirect
        const isSuperAdmin = userData.roles?.includes("SuperAdmin") || false
        const redirectTo = isSuperAdmin ? "/admin" : "/profile"
        
        return { success: true, redirectTo }
      }
      return { success: false }
    } catch (error) {
      console.error("Login error:", error)
      return { success: false }
    }
  }

  const signup = async (
    email: string,
    password: string,
    firstName: string,
    lastName: string
  ): Promise<boolean> => {
    try {
      const fullName = `${firstName} ${lastName}`.trim()
      const response = await api.auth.signup({
        email,
        password,
        fullName,
      }) as {
        id: number
        email: string
        fullName: string
        isActive?: boolean
      }

      if (response.id && response.email) {
        // After signup, automatically log in
        return await login(email, password)
      }
      return false
    } catch (error) {
      console.error("Signup error:", error)
      return false
    }
  }

  const logout = () => {
    setUser(null)
    localStorage.removeItem("currentUser")
    localStorage.removeItem("authToken")
    router.push("/")
  }

  const isSuperAdmin = user?.roles?.includes("SuperAdmin") || false

  return (
    <AuthContext.Provider
      value={{
        user,
        login,
        signup,
        logout,
        isAuthenticated: !!user,
        isInitialized,
        userId: user?.id || null,
        isSuperAdmin,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider")
  }
  return context
}

