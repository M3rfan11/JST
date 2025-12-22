"use client"

import { useState, useEffect, useMemo } from "react"
import { Button } from "./ui/button"
import { Input } from "./ui/input"
import { Label } from "./ui/label"
import { Package, ChevronLeft, ChevronRight, Filter, X } from "lucide-react"
import { api } from "@/lib/api-client"

interface OrderItem {
  id: number
  productId: number
  productName: string
  productSKU?: string
  quantity: number
  unitPrice: number
  totalPrice: number
  unit?: string
}

interface OrderTrackingEvent {
  id: number
  status: string
  notes?: string
  location?: string
  timestamp: string
  updatedByUserName?: string
}

interface Order {
  id: number
  orderNumber: string
  customerName: string
  customerEmail?: string
  customerPhone?: string
  customerAddress: string
  orderDate: string
  deliveryDate?: string
  totalAmount: number
  status: string
  paymentStatus: string
  notes?: string
  items: OrderItem[]
  trackingHistory?: OrderTrackingEvent[] | undefined
}

interface OrdersListProps {
  onOrderClick?: (order: Order) => void
  showDetails?: boolean
  title?: string
}

export function OrdersList({ onOrderClick, showDetails = false, title, className }: OrdersListProps) {
  const [orders, setOrders] = useState<Order[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState("")
  
  // Filters
  const [statusFilter, setStatusFilter] = useState<string>("all")
  const [dateFrom, setDateFrom] = useState("")
  const [dateTo, setDateTo] = useState("")
  
  // Sorting
  const [sortBy, setSortBy] = useState<"date" | "amount" | "status">("date")
  const [sortOrder, setSortOrder] = useState<"asc" | "desc">("desc")
  
  // Pagination
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)

  const statusOptions = ["all", "Pending", "Accepted", "Shipped", "Delivered", "Cancelled"]

  useEffect(() => {
    loadOrders()
  }, [])

  const loadOrders = async () => {
    try {
      setLoading(true)
      setError("")
      const ordersData = (await api.customerOrders.getOrders()) as Order[]
      setOrders(ordersData)
    } catch (error: any) {
      console.error("Error loading orders:", error)
      setError("Failed to load orders.")
    } finally {
      setLoading(false)
    }
  }

  // Filter and sort orders
  const filteredAndSortedOrders = useMemo(() => {
    let result = [...orders]

    // Apply status filter
    if (statusFilter !== "all") {
      result = result.filter((order) => order.status.toLowerCase() === statusFilter.toLowerCase())
    }


    // Apply date range filter
    if (dateFrom) {
      const fromDate = new Date(dateFrom)
      result = result.filter((order) => new Date(order.orderDate) >= fromDate)
    }
    if (dateTo) {
      const toDate = new Date(dateTo)
      toDate.setHours(23, 59, 59, 999) // Include entire end date
      result = result.filter((order) => new Date(order.orderDate) <= toDate)
    }

    // Apply sorting
    result.sort((a, b) => {
      let comparison = 0
      switch (sortBy) {
        case "date":
          comparison = new Date(a.orderDate).getTime() - new Date(b.orderDate).getTime()
          break
        case "amount":
          comparison = a.totalAmount - b.totalAmount
          break
        case "status":
          comparison = a.status.localeCompare(b.status)
          break
      }
      return sortOrder === "asc" ? comparison : -comparison
    })

    return result
  }, [orders, statusFilter, dateFrom, dateTo, sortBy, sortOrder])

  // Pagination
  const totalPages = Math.ceil(filteredAndSortedOrders.length / pageSize)
  const startIndex = (currentPage - 1) * pageSize
  const endIndex = startIndex + pageSize
  const paginatedOrders = filteredAndSortedOrders.slice(startIndex, endIndex)

  // Reset to first page when filters change
  useEffect(() => {
    setCurrentPage(1)
  }, [statusFilter, dateFrom, dateTo, sortBy, sortOrder])

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case "pending":
        return "bg-yellow-100 text-yellow-800"
      case "accepted":
        return "bg-purple-100 text-purple-800"
      case "shipped":
        return "bg-indigo-100 text-indigo-800"
      case "delivered":
        return "bg-green-100 text-green-800"
      case "cancelled":
        return "bg-red-100 text-red-800"
      default:
        return "bg-gray-100 text-gray-800"
    }
  }

  const formatCurrency = (amount: number) => {
    return `$${amount.toFixed(2)}`
  }

  const formatDateTime = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      hour12: true,
    })
  }

  const clearFilters = () => {
    setStatusFilter("all")
    setDateFrom("")
    setDateTo("")
    setSortBy("date")
    setSortOrder("desc")
  }

  const hasActiveFilters = statusFilter !== "all" || dateFrom || dateTo

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading orders...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-center py-12 border border-border">
        <p className="text-destructive" style={{ fontFamily: '"Dream Avenue"' }}>{error}</p>
        <Button onClick={loadOrders} className="mt-4" style={{ fontFamily: '"Dream Avenue"' }}>
          Retry
        </Button>
      </div>
    )
  }

  return (
    <div className={`space-y-6 ${className || ""}`}>
      {title && (
        <h2 className="font-serif text-2xl font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>
          {title}
        </h2>
      )}

      {/* Filters and Sorting */}
      <div className="bg-white rounded-lg border border-border p-4 space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="font-medium flex items-center gap-2" style={{ fontFamily: '"Dream Avenue"' }}>
            <Filter className="h-4 w-4" />
            Filters & Sorting
          </h3>
          {hasActiveFilters && (
            <Button
              variant="ghost"
              size="sm"
              onClick={clearFilters}
              className="text-xs"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              <X className="h-3 w-3 mr-1" />
              Clear Filters
            </Button>
          )}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {/* Status Filter */}
          <div>
            <Label htmlFor="status" className="text-sm mb-2 block" style={{ fontFamily: '"Dream Avenue"' }}>
              Status
            </Label>
            <select
              id="status"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="w-full h-9 px-3 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-[#3D0811]"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              {statusOptions.map((status) => (
                <option key={status} value={status}>
                  {status === "all" ? "All Statuses" : status}
                </option>
              ))}
            </select>
          </div>

          {/* Date From */}
          <div>
            <Label htmlFor="dateFrom" className="text-sm mb-2 block" style={{ fontFamily: '"Dream Avenue"' }}>
              Date From
            </Label>
            <Input
              id="dateFrom"
              type="date"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
              className="h-9"
            />
          </div>

          {/* Date To */}
          <div>
            <Label htmlFor="dateTo" className="text-sm mb-2 block" style={{ fontFamily: '"Dream Avenue"' }}>
              Date To
            </Label>
            <Input
              id="dateTo"
              type="date"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
              className="h-9"
            />
          </div>
        </div>

        {/* Sorting */}
        <div className="flex items-center gap-4 flex-wrap">
          <Label className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
            Sort by:
          </Label>
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as "date" | "amount" | "status")}
            className="h-9 px-3 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-[#3D0811]"
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            <option value="date">Date</option>
            <option value="amount">Amount</option>
            <option value="status">Status</option>
          </select>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setSortOrder(sortOrder === "asc" ? "desc" : "asc")}
            className="h-9"
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            {sortOrder === "asc" ? "↑ Ascending" : "↓ Descending"}
          </Button>
        </div>
      </div>

      {/* Results Count */}
      <div className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
        Showing {startIndex + 1}-{Math.min(endIndex, filteredAndSortedOrders.length)} of{" "}
        {filteredAndSortedOrders.length} order{filteredAndSortedOrders.length !== 1 ? "s" : ""}
      </div>

      {/* Orders List */}
      {paginatedOrders.length === 0 ? (
        <div className="text-center py-12 border border-border">
          <Package className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
          <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
            No orders found
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {paginatedOrders.map((order) => (
            <div
              key={order.id}
              onClick={() => onOrderClick?.(order)}
              className={`border border-border p-6 ${
                onOrderClick ? "cursor-pointer hover:border-foreground transition-colors" : ""
              }`}
            >
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div className="flex-1">
                  <h3 className="font-medium mb-1" style={{ fontFamily: '"Dream Avenue"' }}>
                    Order #{order.orderNumber}
                  </h3>
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    {formatDateTime(order.orderDate)}
                  </p>
                  {showDetails && order.customerAddress && (
                    <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                      {order.customerAddress}
                    </p>
                  )}
                </div>
                <div className="flex items-center gap-4">
                  <span
                    className={`px-3 py-1 rounded-full text-xs font-medium capitalize ${getStatusColor(
                      order.status
                    )}`}
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    {order.status}
                  </span>
                  <span className="font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                    {formatCurrency(order.totalAmount)}
                  </span>
                </div>
              </div>
              {showDetails && order.items && order.items.length > 0 && (
                <div className="mt-4 pt-4 border-t border-border">
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    {order.items.length} item{order.items.length !== 1 ? "s" : ""}
                  </p>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Label className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
              Items per page:
            </Label>
            <select
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value))
                setCurrentPage(1)
              }}
              className="h-9 px-3 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-[#3D0811]"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              <option value="5">5</option>
              <option value="10">10</option>
              <option value="20">20</option>
              <option value="50">50</option>
            </select>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((prev) => Math.max(1, prev - 1))}
              disabled={currentPage === 1}
              className="h-9"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              <ChevronLeft className="h-4 w-4" />
              Previous
            </Button>
            <span className="text-sm px-4" style={{ fontFamily: '"Dream Avenue"' }}>
              Page {currentPage} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((prev) => Math.min(totalPages, prev + 1))}
              disabled={currentPage === totalPages}
              className="h-9"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              Next
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

