"use client"

import { useEffect } from "react"
import { useRouter, usePathname } from "next/navigation"
import { useAuth } from "@/components/auth-provider"

/**
 * Component to redirect SuperAdmin users away from regular pages
 * SuperAdmin can access /admin pages and public pages (home, shop, etc.)
 */
export function SuperAdminGuard({ children }: { children: React.ReactNode }) {
  const { isSuperAdmin, isAuthenticated } = useAuth()
  const router = useRouter()
  const pathname = usePathname()

  useEffect(() => {
    // Only apply guard if user is authenticated and is SuperAdmin
    if (isAuthenticated && isSuperAdmin) {
      // Allow access to admin pages, login/signup, and public pages
      const isAdminPage = pathname?.startsWith("/admin")
      const isLoginPage = pathname === "/login"
      const isSignupPage = pathname === "/signup"
      
      // Public pages that SuperAdmin should be able to access
      const publicPages = [
        "/",
        "/shop",
        "/collections",
        "/about",
        "/track",
        "/cart",
        "/checkout",
        "/product",
      ]
      const isPublicPage = pathname && publicPages.some(page => 
        pathname === page || pathname.startsWith(page + "/")
      )
      
      // If SuperAdmin tries to access non-admin, non-public pages, redirect to admin
      if (!isAdminPage && !isLoginPage && !isSignupPage && !isPublicPage) {
        router.push("/admin")
      }
    }
  }, [isAuthenticated, isSuperAdmin, pathname, router])

  // If SuperAdmin is trying to access a restricted page, show loading while redirecting
  if (isAuthenticated && isSuperAdmin) {
    const isAdminPage = pathname?.startsWith("/admin")
    const isLoginPage = pathname === "/login"
    const isSignupPage = pathname === "/signup"
    const publicPages = [
      "/",
      "/shop",
      "/collections",
      "/about",
      "/track",
      "/cart",
      "/checkout",
      "/product",
    ]
    const isPublicPage = pathname && publicPages.some(page => 
      pathname === page || pathname.startsWith(page + "/")
    )
    
    if (!isAdminPage && !isLoginPage && !isSignupPage && !isPublicPage) {
      return (
        <div className="flex items-center justify-center min-h-screen">
          <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Redirecting to admin dashboard...</div>
        </div>
      )
    }
  }

  return <>{children}</>
}




