"use client"

import { useState, useEffect } from "react"
import { Search, ChevronDown, Eye, ExternalLink } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { api } from "@/lib/api-client"
import { useToast } from "@/hooks/use-toast"
import Image from "next/image"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"

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

interface PaymentProof {
  id: number
  fileUrl: string
  fileType: string
  fileName?: string
  uploadedAt: string
}

interface Order {
  id: number
  orderNumber: string
  customerName: string
  customerEmail?: string
  customerPhone?: string
  customerAddress?: string
  orderDate: string
  deliveryDate?: string
  totalAmount: number
  downPayment?: number
  status: string
  paymentStatus: string
  paymentMethod?: string
  notes?: string
  items: OrderItem[]
  latestPaymentProof?: PaymentProof
}

export default function OrdersPage() {
  const { toast } = useToast()
  const [orders, setOrders] = useState<Order[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState("")
  const [updatingStatus, setUpdatingStatus] = useState<number | null>(null)
  const [viewingProof, setViewingProof] = useState<PaymentProof | null>(null)
  const [showProofDialog, setShowProofDialog] = useState(false)

  useEffect(() => {
    loadOrders()
  }, [])

  const loadOrders = async () => {
    try {
      setLoading(true)
      const response = await api.onlineOrders.getAll() as any
      // Handle different response formats
      let ordersList: any[] = []
      if (Array.isArray(response)) {
        ordersList = response
      } else if (response && Array.isArray(response.value)) {
        ordersList = response.value
      } else if (response && response.data && Array.isArray(response.data)) {
        ordersList = response.data
      }
      
      // Map to Order interface
      const mappedOrders: Order[] = ordersList.map((o: any) => ({
        id: o.id,
        orderNumber: o.orderNumber || o.order_number || `ORD-${o.id}`,
        customerName: o.customerName || o.customer_name || "Unknown",
        customerEmail: o.customerEmail || o.customer_email,
        customerPhone: o.customerPhone || o.customer_phone,
        customerAddress: o.customerAddress || o.customer_address,
        orderDate: o.orderDate || o.order_date || o.createdAt || o.created_at || new Date().toISOString(),
        deliveryDate: o.deliveryDate || o.delivery_date,
        totalAmount: o.totalAmount || o.total_amount || o.total || 0,
        downPayment: o.downPayment || o.down_payment,
        status: o.status || "pending",
        paymentStatus: o.paymentStatus || o.payment_status || "Unpaid",
        paymentMethod: o.paymentMethod || o.payment_method,
        notes: o.notes,
        items: o.items || [],
        latestPaymentProof: o.latestPaymentProof || o.latest_payment_proof,
      }))
      
      setOrders(mappedOrders)
    } catch (error: any) {
      console.error("Error loading orders:", error)
      if (error.status !== 404) {
        toast({
          title: "Error",
          description: "Failed to load orders.",
          variant: "destructive",
        })
      }
    } finally {
      setLoading(false)
    }
  }

  const handleStatusUpdate = async (orderId: number, newStatus: string) => {
    try {
      setUpdatingStatus(orderId)
      const response = await api.onlineOrders.updateStatus(orderId, newStatus) as any
      toast({
        title: "Success",
        description: response?.message || "Order status updated successfully.",
      })
      await loadOrders() // Reload orders to get updated data
    } catch (error: any) {
      console.error("Error updating order status:", error)
      const errorMessage = error?.data?.message || error?.message || "Failed to update order status."
      toast({
        title: "Error",
        description: errorMessage,
        variant: "destructive",
      })
    } finally {
      setUpdatingStatus(null)
    }
  }


  const filteredOrders = orders.filter(
    (order) =>
      order.orderNumber.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.customerName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.customerEmail?.toLowerCase().includes(searchTerm.toLowerCase())
  )

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case "pending":
        return "bg-yellow-100 text-yellow-800"
      case "proof_submitted":
        return "bg-blue-100 text-blue-800"
      case "under_review":
        return "bg-indigo-100 text-indigo-800"
      case "accepted":
        return "bg-purple-100 text-purple-800"
      case "rejected":
        return "bg-red-100 text-red-800"
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

  // Get valid status transitions based on current status and payment method
  const getValidStatusTransitions = (currentStatus: string, paymentMethod?: string): string[] => {
    const status = currentStatus.toLowerCase()
    const isInstaPay = paymentMethod?.toUpperCase() === "INSTAPAY"
    
    let transitions: string[] = []
    
    switch (status) {
      case "pending":
        transitions = ["pending", "accepted", "cancelled"]
        // Only add proof_submitted for InstaPay orders
        if (isInstaPay) {
          transitions.push("proof_submitted")
        }
        return transitions
      case "proof_submitted":
        return ["proof_submitted", "under_review", "accepted", "rejected", "cancelled"]
      case "under_review":
        return ["under_review", "accepted", "rejected", "cancelled"]
      case "accepted":
        return ["accepted", "shipped", "cancelled"]
      case "rejected":
        transitions = ["rejected", "cancelled"]
        // Only add proof_submitted for InstaPay orders
        if (isInstaPay) {
          transitions.push("proof_submitted")
        }
        return transitions
      case "shipped":
        return ["shipped", "delivered", "cancelled"]
      case "delivered":
        return ["delivered"] // No transitions allowed
      case "cancelled":
        return ["cancelled"] // No transitions allowed
      default:
        // For unknown statuses, allow common transitions
        transitions = ["pending", "accepted", "shipped", "delivered", "cancelled"]
        // Only add InstaPay-specific statuses for InstaPay orders
        if (isInstaPay) {
          transitions.push("proof_submitted", "under_review", "rejected")
        }
        return transitions
    }
  }

  const formatCurrency = (amount: number) => {
    return `LE ${amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
  }

  const formatDateTime = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    })
  }

  // Status options removed - using getValidStatusTransitions instead

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading orders...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
          Orders
        </h1>
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          Showing {filteredOrders.length} {filteredOrders.length === 1 ? 'order' : 'orders'}
        </p>
      </div>

      {/* Search */}
      <div className="bg-white rounded-lg p-4 border border-border">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search orders by number, customer name, or email..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10"
            style={{ fontFamily: '"Dream Avenue"' }}
          />
        </div>
      </div>

      {/* Orders Cards */}
      <div className="space-y-4">
        {filteredOrders.length === 0 ? (
          <div className="bg-white rounded-lg p-8 border border-border text-center">
            <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
              No orders found
            </p>
          </div>
        ) : (
          filteredOrders.map((order) => {
            const remainingBalance = order.downPayment 
              ? order.totalAmount - order.downPayment 
              : order.totalAmount

            return (
              <div
                key={order.id}
                className="bg-white rounded-lg border border-border p-6 space-y-4"
              >
                {/* Order Header */}
                <div className="flex items-start justify-between border-b border-border pb-4">
                  <div>
                    <div className="flex items-center gap-3 mb-2">
                      <h3 className="text-lg font-semibold" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                        #{order.orderNumber}
                      </h3>
                      <span
                        className={`px-3 py-1 rounded-full text-xs font-medium ${getStatusColor(order.status)}`}
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {order.status.split('_').map(word => word.charAt(0).toUpperCase() + word.slice(1)).join(' ')}
                      </span>
                    </div>
                    <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {formatDateTime(order.orderDate)}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <select
                      value={order.status}
                      onChange={(e) => handleStatusUpdate(order.id, e.target.value)}
                      disabled={updatingStatus === order.id || 
                               order.status.toLowerCase() === "delivered" || 
                               order.status.toLowerCase() === "cancelled"}
                      className="px-3 py-1.5 text-sm border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-[#3D0811] disabled:opacity-50 disabled:cursor-not-allowed"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      {getValidStatusTransitions(order.status, order.paymentMethod).map((status) => (
                        <option key={status} value={status}>
                          {status.split('_').map(word => word.charAt(0).toUpperCase() + word.slice(1)).join(' ')}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                {/* Order Items */}
                <div className="space-y-2">
                  <h4 className="text-sm font-medium mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                    Order Items
                  </h4>
                  {order.items.map((item) => (
                    <div key={item.id} className="flex items-start justify-between py-2 border-b border-border last:border-0">
                      <div className="flex-1">
                        <p className="font-medium text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                          {item.productName}
                        </p>
                        {item.productSKU && (
                          <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                            SKU: {item.productSKU}
                          </p>
                        )}
                      </div>
                      <div className="text-right">
                        <p className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                          {item.quantity} {item.unit || 'piece'} @ {formatCurrency(item.unitPrice)}
                        </p>
                        <p className="text-sm font-medium mt-1" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                          {formatCurrency(item.totalPrice)}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>

                {/* Customer Information */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-4 border-t border-border">
                  <div>
                    <h4 className="text-sm font-medium mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                      Customer Information
                    </h4>
                    <div className="space-y-1 text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                      <p><span className="text-muted-foreground">Name:</span> {order.customerName}</p>
                      {order.customerEmail && (
                        <p><span className="text-muted-foreground">Email:</span> {order.customerEmail}</p>
                      )}
                      {order.customerPhone && (
                        <p><span className="text-muted-foreground">Phone:</span> {order.customerPhone}</p>
                      )}
                    </div>
                  </div>

                  <div>
                    <h4 className="text-sm font-medium mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                      Shipping Information
                    </h4>
                    <div className="space-y-1 text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                      {order.customerAddress && (
                        <p className="text-muted-foreground">{order.customerAddress}</p>
                      )}
                      {order.notes && (
                        <p className="text-muted-foreground mt-2">
                          <span className="font-medium">Notes:</span> {order.notes}
                        </p>
                      )}
                    </div>
                  </div>
                </div>

                {/* Payment Details */}
                <div className="pt-4 border-t border-border">
                  <h4 className="text-sm font-medium mb-3" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                    Payment Details
                  </h4>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    <div>
                      <p className="text-xs text-muted-foreground mb-1" style={{ fontFamily: '"Dream Avenue"' }}>
                        Total Amount
                      </p>
                      <p className="text-sm font-medium" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                        {formatCurrency(order.totalAmount)}
                      </p>
                    </div>
                    {order.downPayment !== undefined && order.downPayment > 0 && (
                      <>
                        <div>
                          <p className="text-xs text-muted-foreground mb-1" style={{ fontFamily: '"Dream Avenue"' }}>
                            Down Payment
                          </p>
                          <p className="text-sm font-medium" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                            {formatCurrency(order.downPayment)}
                          </p>
                        </div>
                        <div>
                          <p className="text-xs text-muted-foreground mb-1" style={{ fontFamily: '"Dream Avenue"' }}>
                            Remaining Balance
                          </p>
                          <p className="text-sm font-medium" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                            {formatCurrency(remainingBalance)}
                          </p>
                        </div>
                      </>
                    )}
                    <div>
                      <p className="text-xs text-muted-foreground mb-1" style={{ fontFamily: '"Dream Avenue"' }}>
                        Payment Status
                      </p>
                      <p className="text-sm font-medium" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                        {order.paymentStatus}
                      </p>
                    </div>
                    {order.paymentMethod && (
                      <div>
                        <p className="text-xs text-muted-foreground mb-1" style={{ fontFamily: '"Dream Avenue"' }}>
                          Payment Method
                        </p>
                        <p className="text-sm font-medium" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                          {order.paymentMethod}
                        </p>
                      </div>
                    )}
                  </div>
                </div>

                {/* Payment Proof Section (for InstaPay orders) */}
                {order.paymentMethod?.toUpperCase() === "INSTAPAY" && order.latestPaymentProof && (
                  <div className="pt-4 border-t border-border">
                    <div className="flex items-center justify-between mb-3">
                      <h4 className="text-sm font-medium" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                        Payment Proof
                      </h4>
                      <div className="flex gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            setViewingProof(order.latestPaymentProof!)
                            setShowProofDialog(true)
                          }}
                          className="gap-2"
                          style={{ fontFamily: '"Dream Avenue"' }}
                        >
                          <Eye className="h-4 w-4" />
                          View Proof
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            const url = `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081'}${order.latestPaymentProof!.fileUrl}`
                            window.open(url, "_blank")
                          }}
                          className="gap-2"
                          style={{ fontFamily: '"Dream Avenue"' }}
                        >
                          <ExternalLink className="h-4 w-4" />
                          Open
                        </Button>
                      </div>
                    </div>
                    <div className="bg-gray-50 rounded-lg p-3">
                      <div className="flex items-center gap-3">
                        {order.latestPaymentProof.fileType.startsWith("image/") ? (
                          <div className="relative w-20 h-20 bg-white rounded border border-border overflow-hidden flex-shrink-0">
                            <Image
                              src={`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081'}${order.latestPaymentProof.fileUrl}`}
                              alt="Payment Proof"
                              fill
                              className="object-cover cursor-pointer hover:opacity-80"
                              onClick={() => {
                                setViewingProof(order.latestPaymentProof!)
                                setShowProofDialog(true)
                              }}
                              unoptimized
                            />
                          </div>
                        ) : (
                          <div className="w-20 h-20 bg-white rounded border border-border flex items-center justify-center flex-shrink-0">
                            <span className="text-xs text-muted-foreground">PDF</span>
                          </div>
                        )}
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium truncate" style={{ fontFamily: '"Dream Avenue"' }}>
                            {order.latestPaymentProof.fileName || "Payment Proof"}
                          </p>
                          <p className="text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                            Uploaded: {new Date(order.latestPaymentProof.uploadedAt).toLocaleString()}
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )
          })
        )}
      </div>

      {/* Payment Proof View Dialog */}
      <Dialog open={showProofDialog} onOpenChange={setShowProofDialog}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle style={{ fontFamily: '"Dream Avenue"' }}>
              Payment Proof
            </DialogTitle>
            <DialogDescription style={{ fontFamily: '"Dream Avenue"' }}>
              {viewingProof?.fileName || "Payment proof document"}
            </DialogDescription>
          </DialogHeader>
          {viewingProof && (
            <div className="space-y-4">
              <div className="bg-white rounded-lg p-4 flex justify-center items-center min-h-[400px] max-h-[600px] overflow-auto border border-border">
                {viewingProof.fileType.startsWith("image/") ? (
                  <Image
                    src={`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081'}${viewingProof.fileUrl}`}
                    alt="Payment Proof"
                    width={800}
                    height={800}
                    className="object-contain max-w-full max-h-full"
                    unoptimized
                  />
                ) : (
                  <div className="text-center">
                    <p className="text-muted-foreground mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
                      PDF Document
                    </p>
                    <Button
                      variant="outline"
                      onClick={() => {
                        const url = `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081'}${viewingProof.fileUrl}`
                        window.open(url, "_blank")
                      }}
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      <ExternalLink className="h-4 w-4 mr-2" />
                      Open PDF in New Tab
                    </Button>
                  </div>
                )}
              </div>
              <div className="text-sm text-muted-foreground space-y-1" style={{ fontFamily: '"Dream Avenue"' }}>
                <p><strong>File Type:</strong> {viewingProof.fileType}</p>
                {viewingProof.fileName && <p><strong>File Name:</strong> {viewingProof.fileName}</p>}
                <p><strong>Uploaded:</strong> {new Date(viewingProof.uploadedAt).toLocaleString()}</p>
              </div>
              <div className="flex justify-end">
                <Button
                  variant="outline"
                  onClick={() => setShowProofDialog(false)}
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  Close
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
