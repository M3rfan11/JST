"use client"

import { useState, useEffect } from "react"
import { Button } from "./ui/button"
import { useAuth } from "./auth-provider"
import { Package, ChevronRight, Truck, CheckCircle, MapPin, Clock } from "lucide-react"

interface Order {
  id: string
  date: string
  total: number
  status: string
  userId?: string | null
  trackingNumber?: string
  trackingEvents?: {
    status: string
    location: string
    date: string
    description: string
  }[]
  shipping: {
    firstName: string
    lastName: string
    address: string
    city: string
    state: string
    zipCode: string
  }
}

export function OrderHistory() {
  const { user } = useAuth()
  const [orders, setOrders] = useState<Order[]>([])
  const [expandedOrder, setExpandedOrder] = useState<string | null>(null)

  useEffect(() => {
    const saved = localStorage.getItem("orders")
    if (saved) {
      const allOrders = JSON.parse(saved)
      // Filter orders by user ID if user is logged in
      const userOrders = user 
        ? allOrders.filter((order: Order) => order.userId === user.id)
        : allOrders.filter((order: Order) => !order.userId) // Show guest orders if not logged in
      
      const parsedOrders = userOrders.map((order: Order) => ({
        ...order,
        trackingNumber: order.trackingNumber || `TH${Math.random().toString(36).substring(2, 10).toUpperCase()}`,
        trackingEvents: order.trackingEvents || generateMockTracking(order.status, order.date),
      }))
      setOrders(parsedOrders)
    }
  }, [user])

  const generateMockTracking = (status: string, orderDate: string) => {
    const events = [
      {
        status: "confirmed",
        location: "Thudarum Headquarters",
        date: orderDate,
        description: "Order confirmed and payment received",
      },
    ]

    if (status === "processing" || status === "shipped" || status === "delivered") {
      events.push({
        status: "processing",
        location: "Manufacturing Facility",
        date: new Date(new Date(orderDate).getTime() + 24 * 60 * 60 * 1000).toISOString(),
        description: "Item prepared for shipment",
      })
    }

    if (status === "shipped" || status === "delivered") {
      events.push({
        status: "shipped",
        location: "Distribution Center",
        date: new Date(new Date(orderDate).getTime() + 48 * 60 * 60 * 1000).toISOString(),
        description: "Package picked up by courier",
      })
      events.push({
        status: "in-transit",
        location: "Regional Hub",
        date: new Date(new Date(orderDate).getTime() + 72 * 60 * 60 * 1000).toISOString(),
        description: "In transit to your location",
      })
    }

    if (status === "delivered") {
      events.push({
        status: "delivered",
        location: "Your Address",
        date: new Date(new Date(orderDate).getTime() + 96 * 60 * 60 * 1000).toISOString(),
        description: "Successfully delivered",
      })
    }

    return events
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case "processing":
        return "text-blue-600"
      case "shipped":
        return "text-green-600"
      case "delivered":
        return "text-gray-600"
      default:
        return "text-muted-foreground"
    }
  }

  const getTrackingIcon = (status: string) => {
    switch (status) {
      case "confirmed":
        return CheckCircle
      case "processing":
        return Package
      case "shipped":
      case "in-transit":
        return Truck
      case "delivered":
        return MapPin
      default:
        return Clock
    }
  }

  return (
    <div className="max-w-4xl">
      <h2 className="font-serif text-2xl font-semibold mb-6" style={{ fontFamily: '"Dream Avenue"' }}>Order History</h2>
      <div className="space-y-4">
        {orders.map((order) => {
          const isExpanded = expandedOrder === order.id
          const Icon = getTrackingIcon(order.status)

          return (
            <div key={order.id} className="border border-border hover:border-foreground transition-colors">
              <div className="p-6">
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-4">
                  <div>
                    <h3 className="font-medium mb-1" style={{ fontFamily: '"Dream Avenue"' }}>Order #{order.id}</h3>
                    <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {new Date(order.date).toLocaleDateString("en-US", {
                        year: "numeric",
                        month: "long",
                        day: "numeric",
                      })}
                    </p>
                    {order.trackingNumber && (
                      <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>Tracking: {order.trackingNumber}</p>
                    )}
                  </div>
                  <div className="flex items-center gap-4">
                    <span className={`text-sm font-medium capitalize ${getStatusColor(order.status)}`} style={{ fontFamily: '"Dream Avenue"' }}>
                      {order.status}
                    </span>
                    <span className="font-medium" style={{ fontFamily: '"Dream Avenue"' }}>${order.total.toFixed(2)}</span>
                  </div>
                </div>

                <div className="text-sm text-muted-foreground mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
                  <p>
                    Shipping to: {order.shipping.firstName} {order.shipping.lastName}
                  </p>
                  <p>
                    {order.shipping.address}, {order.shipping.city}, {order.shipping.state} {order.shipping.zipCode}
                  </p>
                </div>

                <Button
                  variant="ghost"
                  className="group w-full sm:w-auto"
                  onClick={() => setExpandedOrder(isExpanded ? null : order.id)}
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  {isExpanded ? "Hide" : "Show"} Tracking
                  <ChevronRight
                    className={`ml-2 h-4 w-4 transition-transform ${isExpanded ? "rotate-90" : "group-hover:translate-x-1"}`}
                  />
                </Button>
              </div>

              {isExpanded && order.trackingEvents && (
                <div className="border-t border-border bg-muted/30 p-6">
                  <h4 className="font-medium mb-4 flex items-center gap-2" style={{ fontFamily: '"Dream Avenue"' }}>
                    <Truck className="h-4 w-4" />
                    Shipment Tracking
                  </h4>
                  <div className="space-y-4">
                    {order.trackingEvents.map((event, index) => {
                      const EventIcon = getTrackingIcon(event.status)
                      const isLast = index === order.trackingEvents!.length - 1

                      return (
                        <div key={index} className="flex gap-4">
                          <div className="flex flex-col items-center">
                            <div
                              className={`flex h-8 w-8 items-center justify-center rounded-full ${
                                isLast
                                  ? "bg-foreground text-background"
                                  : "bg-muted-foreground/20 text-muted-foreground"
                              }`}
                            >
                              <EventIcon className="h-4 w-4" />
                            </div>
                            {!isLast && <div className="w-0.5 flex-1 bg-border my-1 min-h-[20px]" />}
                          </div>
                          <div className="flex-1 pb-4">
                            <p className="font-medium capitalize" style={{ fontFamily: '"Dream Avenue"' }}>{event.status.replace("-", " ")}</p>
                            <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>{event.description}</p>
                            <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                              {event.location} •{" "}
                              {new Date(event.date).toLocaleDateString("en-US", {
                                month: "short",
                                day: "numeric",
                                hour: "numeric",
                                minute: "2-digit",
                              })}
                            </p>
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </div>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
