"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import { 
  LayoutDashboard, 
  Package, 
  ShoppingCart, 
  Users, 
  Tag, 
  Warehouse,
  BarChart3,
  Settings as SettingsIcon,
  Menu,
  X,
  LogOut
} from "lucide-react"
import { useAuth } from "@/components/auth-provider"
import { Button } from "@/components/ui/button"

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [isChecking, setIsChecking] = useState(true)
  const pathname = usePathname()
  const router = useRouter()
  const { logout, isAuthenticated, isSuperAdmin } = useAuth()

  useEffect(() => {
    if (!isAuthenticated) {
      router.push("/login?redirect=/admin")
    } else if (!isSuperAdmin) {
      // If user is authenticated but not SuperAdmin, redirect to profile
      router.push("/profile")
    } else {
      setIsChecking(false)
    }
  }, [isAuthenticated, isSuperAdmin, router])

  if (isChecking || !isAuthenticated || !isSuperAdmin) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading...</div>
      </div>
    )
  }

  const menuItems = [
    { href: "/admin", label: "Dashboard", icon: LayoutDashboard },
    { href: "/admin/products", label: "Products", icon: Package },
    { href: "/admin/orders", label: "Orders", icon: ShoppingCart },
    { href: "/admin/categories", label: "Categories", icon: Tag },
    { href: "/admin/users", label: "Users & Roles", icon: Users },
    { href: "/admin/promo-codes", label: "Promo Codes", icon: Tag },
    { href: "/admin/inventory", label: "Inventory", icon: Warehouse },
    { href: "/admin/reports", label: "Reports", icon: BarChart3 },
    { href: "/admin/settings", label: "Settings", icon: SettingsIcon },
  ]

  const handleLogout = () => {
    logout()
    router.push("/")
  }

  return (
    <div className="min-h-screen" style={{ backgroundColor: 'rgba(206, 180, 157, 1)' }}>
      {/* Mobile Header */}
      <header className="lg:hidden fixed top-0 left-0 right-0 z-50" style={{ backgroundColor: '#3D0811' }}>
        <div className="flex items-center justify-between p-4">
          <Link href="/admin" className="text-white text-xl font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>
            JST Admin
          </Link>
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setSidebarOpen(!sidebarOpen)}
            className="text-white"
          >
            {sidebarOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
          </Button>
        </div>
      </header>

      <div className="flex">
        {/* Sidebar */}
        <aside
          className={`fixed inset-y-0 left-0 z-40 w-64 h-screen transition-transform duration-300 ${
            sidebarOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"
          }`}
          style={{ backgroundColor: '#3D0811' }}
        >
          <div className="h-full flex flex-col">
            {/* Logo/Title */}
            <div className="p-6 border-b flex-shrink-0" style={{ borderColor: 'rgba(255, 255, 255, 0.1)' }}>
              <Link href="/admin" className="text-white text-2xl font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>
                JST Admin
              </Link>
            </div>

            {/* Navigation */}
            <nav className="p-4 flex-shrink-0" style={{ backgroundColor: '#3D0811' }}>
              <ul className="space-y-2">
                {menuItems.map((item) => {
                  const Icon = item.icon
                  const isActive = pathname === item.href || (item.href !== "/admin" && pathname.startsWith(item.href))
                  
                  return (
                    <li key={item.href}>
                      <Link
                        href={item.href}
                        onClick={() => setSidebarOpen(false)}
                        className={`flex items-center gap-3 px-4 py-3 rounded-md transition-colors ${
                          isActive
                            ? "text-white"
                            : "text-white/70 hover:text-white hover:bg-white/10"
                        }`}
                        style={{
                          backgroundColor: isActive ? 'rgba(255, 255, 255, 0.1)' : 'transparent',
                          fontFamily: '"Dream Avenue"'
                        }}
                      >
                        <Icon className="h-5 w-5" />
                        <span>{item.label}</span>
                      </Link>
                    </li>
                  )
                })}
                
                {/* Footer Actions - Moved inside nav */}
                <li className="border-t pt-2 mt-2" style={{ borderColor: 'rgba(255, 255, 255, 0.1)' }}>
                  <Link
                    href="/"
                    onClick={() => setSidebarOpen(false)}
                    className="flex items-center gap-3 px-4 py-3 rounded-md text-white/70 hover:text-white hover:bg-white/10 transition-colors"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    <span>← Back to Site</span>
                  </Link>
                </li>
                <li>
                  <button
                    onClick={handleLogout}
                    className="flex items-center gap-3 px-4 py-3 rounded-md text-white/70 hover:text-white hover:bg-white/10 transition-colors w-full text-left"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    <LogOut className="h-5 w-5" />
                    <span>Logout</span>
                  </button>
                </li>
              </ul>
            </nav>
            
            {/* Empty space with main background color */}
            <div className="flex-1" style={{ backgroundColor: 'rgba(206, 180, 157, 1)' }}></div>
          </div>
        </aside>

        {/* Overlay for mobile */}
        {sidebarOpen && (
          <div
            className="fixed inset-0 bg-black/50 z-30 lg:hidden"
            onClick={() => setSidebarOpen(false)}
          />
        )}

        {/* Main Content */}
        <main className="flex-1 lg:ml-64 pt-16 lg:pt-0">
          <div className="p-6 lg:p-8">
            {children}
          </div>
        </main>
      </div>
    </div>
  )
}

