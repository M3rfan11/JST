"use client"

import { useState, useEffect } from "react"
import { Header } from "@/components/header"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import Link from "next/link"
import { ArrowLeft, Package, Truck, CheckCircle, MapPin, Clock, Search } from "lucide-react"

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

export default function TrackOrderPage() {
  const [orderId, setOrderId] = useState("")
  const [order, setOrder] = useState<Order | null>(null)
  const [error, setError] = useState("")
  const [isSearching, setIsSearching] = useState(false)

  const generateMockTracking = (status: string, orderDate: string) => {
    const events = [
      {
        status: "confirmed",
        location: "JST Headquarters",
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

  const handleSearch = () => {
    if (!orderId.trim()) {
      setError("Please enter an order ID")
      return
    }

    setIsSearching(true)
    setError("")

    // Simulate search delay
    setTimeout(() => {
      const saved = localStorage.getItem("orders")
      if (saved) {
        const orders = JSON.parse(saved)
        const foundOrder = orders.find((o: Order) => o.id.toLowerCase() === orderId.toLowerCase().trim())

        if (foundOrder) {
          const orderWithTracking: Order = {
            ...foundOrder,
            trackingNumber: foundOrder.trackingNumber || `JST${Math.random().toString(36).substring(2, 10).toUpperCase()}`,
            trackingEvents: foundOrder.trackingEvents || generateMockTracking(foundOrder.status, foundOrder.date),
          }
          setOrder(orderWithTracking)
        } else {
          setError("Order not found. Please check your order ID and try again.")
          setOrder(null)
        }
      } else {
        setError("No orders found. Please check your order ID and try again.")
        setOrder(null)
      }
      setIsSearching(false)
    }, 500)
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      handleSearch()
    }
  }

  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 pt-24 sm:pt-28 md:pt-32 pb-8 md:pb-12 max-w-4xl">
        <Button variant="ghost" asChild className="mb-6" style={{ fontFamily: '"Dream Avenue"' }}>
          <Link href="/">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Link>
        </Button>

        <div className="mb-8">
          <h1 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
            Track Your Order
          </h1>
          <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
            Enter your order ID to track your shipment
          </p>
        </div>

        {/* Search Form */}
        <div className="mb-8">
          <div className="flex gap-2">
            <div className="flex-1">
              <Label htmlFor="orderId" className="sr-only">Order ID</Label>
              <Input
                id="orderId"
                type="text"
                placeholder="Enter your order ID (e.g., abc123xyz)"
                value={orderId}
                onChange={(e) => setOrderId(e.target.value)}
                onKeyPress={handleKeyPress}
                className="h-12"
              />
            </div>
            <Button
              onClick={handleSearch}
              disabled={isSearching}
              size="lg"
              className="h-12 px-8"
              style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
            >
              <Search className="h-4 w-4 mr-2" />
              {isSearching ? "Searching..." : "Track"}
            </Button>
          </div>
          {error && (
            <p className="text-destructive text-sm mt-2" style={{ fontFamily: '"Dream Avenue"' }}>
              {error}
            </p>
          )}
        </div>

        {/* Order Details */}
        {order && (
          <div className="border border-border p-6 sm:p-8">
            <div className="mb-6">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-4">
                <div>
                  <h2 className="font-medium mb-1" style={{ fontFamily: '"Dream Avenue"' }}>Order #{order.id}</h2>
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    {new Date(order.date).toLocaleDateString("en-US", {
                      year: "numeric",
                      month: "long",
                      day: "numeric",
                    })}
                  </p>
                  {order.trackingNumber && (
                    <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                      Tracking: {order.trackingNumber}
                    </p>
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
            </div>

            {/* Tracking Timeline */}
            {order.trackingEvents && order.trackingEvents.length > 0 && (
              <div className="border-t border-border pt-6">
                <h3 className="font-medium mb-4 flex items-center gap-2" style={{ fontFamily: '"Dream Avenue"' }}>
                  <Truck className="h-4 w-4" />
                  Shipment Tracking
                </h3>
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
                          <p className="font-medium capitalize" style={{ fontFamily: '"Dream Avenue"' }}>
                            {event.status.replace("-", " ")}
                          </p>
                          <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                            {event.description}
                          </p>
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
        )}

        {!order && !error && (
          <div className="text-center py-12 border border-border">
            <Package className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
            <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
              Enter an order ID above to track your order
            </p>
          </div>
        )}
      </div>
    </div>
  )
}

