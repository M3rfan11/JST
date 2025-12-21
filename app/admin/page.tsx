"use client"

import { useState, useEffect } from "react"
import { Package, Users, ShoppingCart, DollarSign, TrendingUp, AlertTriangle, Activity } from "lucide-react"
import { api } from "@/lib/api-client"
import Link from "next/link"
import { Button } from "@/components/ui/button"

interface DashboardStats {
  totalProducts: number
  totalUsers: number
  totalInventory: number
  pendingPurchases: number
  pendingSales: number
  lowStockItems: number
  totalRevenue: number
  totalCosts: number
  pendingRequests: number
  completedAssemblies: number
}

interface RecentActivity {
  id: number
  type: string
  title: string
  description: string
  status: string
  createdAt: string
  userName: string
}

interface RecentOrder {
  id: number
  orderNumber: string
  customerName: string
  totalAmount: number
  status: string
  orderDate: string
}

export default function AdminDashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null)
  const [recentActivity, setRecentActivity] = useState<RecentActivity[]>([])
  const [lowStockItems, setLowStockItems] = useState<any[]>([])
  const [recentOrders, setRecentOrders] = useState<RecentOrder[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadDashboardData()
  }, [])

  const loadDashboardData = async () => {
    try {
      setLoading(true)
      // Load dashboard stats
      const statsData = await api.dashboard.getStats() as DashboardStats
      setStats(statsData)

      // Load recent activity
      const activityData = await api.dashboard.getRecentActivity() as RecentActivity[]
      setRecentActivity(activityData)

      // Load low stock items
      try {
        const lowStockData = await api.dashboard.getLowStockItems() as any[]
        console.log("📦 Low stock items from API:", {
          count: lowStockData?.length || 0,
          items: lowStockData,
        })
        setLowStockItems(Array.isArray(lowStockData) ? lowStockData : [])
      } catch (error: any) {
        console.error("❌ Error loading low stock items:", error)
        setLowStockItems([])
      }

      // Load recent orders
      try {
        const ordersResponse = await api.onlineOrders.getAll() as any
        let ordersList: any[] = []
        if (Array.isArray(ordersResponse)) {
          ordersList = ordersResponse
        } else if (ordersResponse && Array.isArray(ordersResponse.value)) {
          ordersList = ordersResponse.value
        } else if (ordersResponse && ordersResponse.data && Array.isArray(ordersResponse.data)) {
          ordersList = ordersResponse.data
        }
        
        // Map to RecentOrder interface and take only the 5 most recent
        const mappedOrders: RecentOrder[] = ordersList
          .map((o: any) => ({
            id: o.id,
            orderNumber: o.orderNumber || o.order_number || `ORD-${o.id}`,
            customerName: o.customerName || o.customer_name || "Unknown",
            totalAmount: o.totalAmount || o.total_amount || o.total || 0,
            status: o.status || "pending",
            orderDate: o.orderDate || o.order_date || o.createdAt || o.created_at || new Date().toISOString(),
          }))
          .sort((a, b) => new Date(b.orderDate).getTime() - new Date(a.orderDate).getTime())
          .slice(0, 5)
        
        setRecentOrders(mappedOrders)
      } catch (orderError) {
        console.error("Error loading recent orders:", orderError)
        setRecentOrders([])
      }
    } catch (error) {
      console.error("Error loading dashboard data:", error)
      // Set empty defaults on error
      setStats({
        totalProducts: 0,
        totalUsers: 0,
        totalInventory: 0,
        pendingPurchases: 0,
        pendingSales: 0,
        lowStockItems: 0,
        totalRevenue: 0,
        totalCosts: 0,
        pendingRequests: 0,
        completedAssemblies: 0,
      })
      setRecentActivity([])
      setLowStockItems([])
      setRecentOrders([])
    } finally {
      setLoading(false)
    }
  }

  const statCards = [
    {
      title: "Total Products",
      value: stats?.totalProducts || 0,
      icon: Package,
      color: "bg-blue-500",
    },
    {
      title: "Total Users",
      value: stats?.totalUsers || 0,
      icon: Users,
      color: "bg-green-500",
    },
    {
      title: "Total Revenue",
      value: `${stats?.totalRevenue.toFixed(2) || "0.00"} EGP`,
      icon: DollarSign,
      color: "bg-purple-500",
    },
    {
      title: "Low Stock Items",
      value: stats?.lowStockItems || 0,
      icon: AlertTriangle,
      color: "bg-red-500",
    },
    {
      title: "Pending Orders",
      value: (stats?.pendingSales || 0) + (stats?.pendingPurchases || 0),
      icon: ShoppingCart,
      color: "bg-orange-500",
    },
    {
      title: "Total Inventory",
      value: stats?.totalInventory || 0,
      icon: TrendingUp,
      color: "bg-indigo-500",
    },
  ]

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading dashboard...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
          Dashboard
        </h1>
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          Welcome to JST Admin Dashboard
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {statCards.map((card, index) => {
          const Icon = card.icon
          return (
            <div
              key={index}
              className="bg-white rounded-lg p-6 shadow-sm border border-border"
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground mb-1" style={{ fontFamily: '"Dream Avenue"' }}>
                    {card.title}
                  </p>
                  <p className="text-2xl font-semibold" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                    {card.value}
                  </p>
                </div>
                <div className={`${card.color} p-3 rounded-lg`}>
                  <Icon className="h-6 w-6 text-white" />
                </div>
              </div>
            </div>
          )
        })}
      </div>

      {/* Recent Orders, Activity & Low Stock */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Recent Orders */}
        <div className="bg-white rounded-lg p-6 shadow-sm border border-border">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold flex items-center gap-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              <ShoppingCart className="h-5 w-5" />
              Recent Orders
            </h2>
            <Button variant="ghost" size="sm" asChild>
              <Link href="/admin/orders" style={{ fontFamily: '"Dream Avenue"', fontSize: '0.75rem' }}>
                View All
              </Link>
            </Button>
          </div>
          <div className="space-y-4">
            {recentOrders.length === 0 ? (
              <p className="text-muted-foreground text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                No recent orders
              </p>
            ) : (
              recentOrders.map((order) => {
                const getStatusColor = (status: string) => {
                  switch (status.toLowerCase()) {
                    case "pending":
                      return "bg-yellow-100 text-yellow-800"
                    case "processing":
                      return "bg-blue-100 text-blue-800"
                    case "shipped":
                      return "bg-purple-100 text-purple-800"
                    case "delivered":
                      return "bg-green-100 text-green-800"
                    case "cancelled":
                      return "bg-red-100 text-red-800"
                    default:
                      return "bg-gray-100 text-gray-800"
                  }
                }
                return (
                  <div key={order.id} className="border-b border-border pb-3 last:border-0">
                    <div className="flex items-center justify-between mb-1">
                      <Link 
                        href={`/admin/orders/${order.id}`}
                        className="font-medium text-sm hover:underline" 
                        style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}
                      >
                        #{order.orderNumber}
                      </Link>
                      <span
                        className={`px-2 py-1 rounded text-xs capitalize ${getStatusColor(order.status)}`}
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {order.status}
                      </span>
                    </div>
                    <p className="text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {order.customerName}
                    </p>
                    <p className="text-xs font-medium mt-1" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                      {order.totalAmount.toFixed(2)} EGP
                    </p>
                    <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                      {new Date(order.orderDate).toLocaleDateString()}
                    </p>
                  </div>
                )
              })
            )}
          </div>
        </div>

        {/* Recent Activity */}
        <div className="bg-white rounded-lg p-6 shadow-sm border border-border">
          <h2 className="text-xl font-semibold mb-4 flex items-center gap-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            <Activity className="h-5 w-5" />
            Recent Activity
          </h2>
          <div className="space-y-4">
            {recentActivity.length === 0 ? (
              <p className="text-muted-foreground text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                No recent activity
              </p>
            ) : (
              recentActivity.slice(0, 5).map((activity) => (
                <div key={activity.id} className="border-b border-border pb-3 last:border-0">
                  <p className="font-medium text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                    {activity.title}
                  </p>
                  <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                    {activity.description}
                  </p>
                  <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                    {new Date(activity.createdAt).toLocaleDateString()} by {activity.userName}
                  </p>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Low Stock Items */}
        <div className="bg-white rounded-lg p-6 shadow-sm border border-border">
          <h2 className="text-xl font-semibold mb-4 flex items-center gap-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            <AlertTriangle className="h-5 w-5" />
            Low Stock Items
          </h2>
          <div className="space-y-4">
            {lowStockItems.length === 0 ? (
              <p className="text-muted-foreground text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                No low stock items
              </p>
            ) : (
              lowStockItems.slice(0, 5).map((item) => (
                <div key={`${item.type}-${item.id}`} className="border-b border-border pb-3 last:border-0">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                        {item.productName}
                        {item.type === "Variant" && item.variantAttributes && (
                          <span className="text-xs text-muted-foreground ml-2">
                            ({item.variantAttributes})
                          </span>
                        )}
                      </p>
                      <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                        Qty: {item.quantity} {item.unit || 'piece'}
                        {item.minimumStockLevel != null && (
                          <span className="ml-2">(Min: {item.minimumStockLevel})</span>
                        )}
                      </p>
                      {item.warehouseName && (
                        <p className="text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                          {item.warehouseName}
                        </p>
                      )}
                    </div>
                    <span
                      className={`px-2 py-1 rounded text-xs ${
                        item.severity === "Critical"
                          ? "bg-red-100 text-red-800"
                          : item.severity === "High"
                          ? "bg-orange-100 text-orange-800"
                          : "bg-yellow-100 text-yellow-800"
                      }`}
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      {item.severity}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

