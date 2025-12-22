"use client";

import { useState, useEffect } from "react";
import { useSearchParams } from "next/navigation";
import { Header } from "@/components/header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import Link from "next/link";
import {
  ArrowLeft,
  Package,
  Truck,
  CheckCircle,
  MapPin,
  Clock,
  Search,
} from "lucide-react";
import { api } from "@/lib/api-client";
import { useAuth } from "@/components/auth-provider";
import { useToast } from "@/hooks/use-toast";

interface OrderTrackingEvent {
  id: number;
  status: string;
  notes?: string;
  location?: string;
  timestamp: string;
  updatedByUserName?: string;
}

interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  productSKU?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  unit?: string;
}

interface Order {
  id: number;
  orderNumber: string;
  customerName: string;
  customerEmail?: string;
  customerPhone?: string;
  customerAddress: string;
  orderDate: string;
  deliveryDate?: string;
  totalAmount: number;
  status: string;
  paymentStatus: string;
  notes?: string;
  items: OrderItem[];
  trackingHistory: OrderTrackingEvent[];
}

export default function TrackOrderPage() {
  const searchParams = useSearchParams();
  const { user, isAuthenticated } = useAuth();
  const { toast } = useToast();
  const [orderNumber, setOrderNumber] = useState("");
  const [email, setEmail] = useState("");
  const [order, setOrder] = useState<Order | null>(null);
  const [error, setError] = useState("");
  const [isSearching, setIsSearching] = useState(false);

  // Load order from URL params if available
  useEffect(() => {
    const orderIdParam = searchParams?.get("orderId");
    const emailParam = searchParams?.get("email");

    if (orderIdParam) {
      setOrderNumber(orderIdParam);
      if (emailParam) {
        setEmail(emailParam);
        // Auto-fetch if both are provided
        const fetchOrder = async () => {
          setIsSearching(true);
          setError("");
          setOrder(null);
          try {
            const orderData = (await api.customerOrders.trackOrderByNumber(
              orderIdParam,
              emailParam
            )) as Order;
            setOrder(orderData);
          } catch (error: any) {
            setError(
              error.message ||
                "Order not found. Please check your order number and email."
            );
          } finally {
            setIsSearching(false);
          }
        };
        fetchOrder();
      } else if (isAuthenticated && user?.email) {
        setEmail(user.email);
        // Auto-fetch for authenticated users (no email needed)
        const fetchOrder = async () => {
          setIsSearching(true);
          setError("");
          setOrder(null);
          try {
            const orderData = (await api.customerOrders.trackOrderByNumber(
              orderIdParam
            )) as Order;
            setOrder(orderData);
          } catch (error: any) {
            setError(
              error.message ||
                "Order not found. Please check your order number."
            );
          } finally {
            setIsSearching(false);
          }
        };
        fetchOrder();
      }
    }
  }, [searchParams, isAuthenticated, user]);

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case "processing":
      case "accepted":
        return "text-blue-600";
      case "shipped":
        return "text-green-600";
      case "delivered":
        return "text-gray-600";
      case "cancelled":
        return "text-red-600";
      default:
        return "text-muted-foreground";
    }
  };

  const getTrackingIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case "pending":
      case "confirmed":
        return CheckCircle;
      case "processing":
      case "accepted":
        return Package;
      case "shipped":
      case "in-transit":
        return Truck;
      case "delivered":
        return MapPin;
      case "cancelled":
        return Clock;
      default:
        return Clock;
    }
  };

  const handleSearch = async () => {
    const orderNumToSearch = orderNumber.trim();

    if (!orderNumToSearch) {
      setError("Please enter an order number");
      return;
    }

    // For guest users, email is required
    if (!isAuthenticated && !email.trim()) {
      setError("Please enter your email address");
      return;
    }

    setIsSearching(true);
    setError("");
    setOrder(null);

    try {
      // For authenticated users, we can track without email
      // For guest users, we need email
      const orderData = (await api.customerOrders.trackOrderByNumber(
        orderNumToSearch,
        isAuthenticated && user?.email ? undefined : email.trim()
      )) as Order;
      setOrder(orderData);
      toast({
        title: "Order found",
        description: `Order #${orderData.orderNumber} retrieved successfully.`,
      });
    } catch (error: any) {
      console.error("Error tracking order:", error);
      setError(
        error.message ||
          "Order not found. Please check your order number and email."
      );
      setOrder(null);
      toast({
        title: "Error",
        description:
          error.message || "Failed to track order. Please try again.",
        variant: "destructive",
      });
    } finally {
      setIsSearching(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      handleSearch();
    }
  };

  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 pt-24 sm:pt-28 md:pt-32 pb-8 md:pb-12 max-w-4xl">
        <Button
          variant="ghost"
          asChild
          className="mb-6"
          style={{ fontFamily: '"Dream Avenue"' }}
        >
          <Link href="/">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Link>
        </Button>

        <div className="mb-8">
          <h1
            className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-4"
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            Track Your Order
          </h1>
          <p
            className="text-muted-foreground"
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            Enter your order ID to track your shipment
          </p>
        </div>

        {/* Search Form */}
        <div className="mb-8">
          <div className="space-y-4">
            <div>
              <Label
                htmlFor="orderNumber"
                className="text-sm mb-2 block"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                Order Number
              </Label>
              <Input
                id="orderNumber"
                type="text"
                placeholder="Enter your order number (e.g., CUST202412010001)"
                value={orderNumber}
                onChange={(e) => setOrderNumber(e.target.value)}
                onKeyPress={handleKeyPress}
                className="h-12"
              />
            </div>
            <div>
              <Label
                htmlFor="email"
                className="text-sm mb-2 block"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                Email Address
              </Label>
              <Input
                id="email"
                type="email"
                placeholder="Enter your email address"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                onKeyPress={handleKeyPress}
                className="h-12"
                disabled={isAuthenticated && !!user?.email}
              />
            </div>
            <Button
              onClick={handleSearch}
              disabled={
                isSearching ||
                !orderNumber.trim() ||
                (!isAuthenticated && !email.trim())
              }
              size="lg"
              className="w-full h-12"
              style={{
                backgroundColor: "#3D0811",
                color: "rgba(255, 255, 255, 1)",
                fontFamily: '"Dream Avenue"',
              }}
            >
              <Search className="h-4 w-4 mr-2" />
              {isSearching ? "Searching..." : "Track Order"}
            </Button>
          </div>
          {error && (
            <p
              className="text-destructive text-sm mt-2"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
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
                  <h2
                    className="font-medium mb-1"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Order #{order.orderNumber}
                  </h2>
                  <p
                    className="text-sm text-muted-foreground"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    {new Date(order.orderDate).toLocaleDateString("en-US", {
                      year: "numeric",
                      month: "long",
                      day: "numeric",
                    })}
                  </p>
                </div>
                <div className="flex items-center gap-4">
                  <span
                    className={`text-sm font-medium capitalize ${getStatusColor(
                      order.status
                    )}`}
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    {order.status}
                  </span>
                  <span
                    className="font-medium"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    ${order.totalAmount.toFixed(2)}
                  </span>
                </div>
              </div>

              <div
                className="text-sm text-muted-foreground mb-4 space-y-1"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                <p>
                  <strong>Customer:</strong> {order.customerName}
                </p>
                {order.customerEmail && (
                  <p>
                    <strong>Email:</strong> {order.customerEmail}
                  </p>
                )}
                {order.customerPhone && (
                  <p>
                    <strong>Phone:</strong> {order.customerPhone}
                  </p>
                )}
                <p>
                  <strong>Shipping Address:</strong> {order.customerAddress}
                </p>
                {order.deliveryDate && (
                  <p>
                    <strong>Expected Delivery:</strong>{" "}
                    {new Date(order.deliveryDate).toLocaleDateString("en-US", {
                      year: "numeric",
                      month: "long",
                      day: "numeric",
                    })}
                  </p>
                )}
              </div>

              {/* Order Items */}
              {order.items && order.items.length > 0 && (
                <div className="border-t border-border pt-4 mb-4">
                  <h3
                    className="font-medium mb-3"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Order Items
                  </h3>
                  <div className="space-y-2">
                    {order.items.map((item) => (
                      <div
                        key={item.id}
                        className="flex justify-between text-sm"
                      >
                        <span style={{ fontFamily: '"Dream Avenue"' }}>
                          {item.productName} x {item.quantity}{" "}
                          {item.unit || "piece"}
                        </span>
                        <span style={{ fontFamily: '"Dream Avenue"' }}>
                          ${item.totalPrice.toFixed(2)}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>

            {/* Tracking Timeline */}
            {order.trackingHistory && order.trackingHistory.length > 0 && (
              <div className="border-t border-border pt-6">
                <h3
                  className="font-medium mb-4 flex items-center gap-2"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  <Truck className="h-4 w-4" />
                  Order Tracking History
                </h3>
                <div className="space-y-4">
                  {order.trackingHistory.map((event, index) => {
                    const EventIcon = getTrackingIcon(event.status);
                    const isLast = index === order.trackingHistory!.length - 1;

                    return (
                      <div key={event.id} className="flex gap-4">
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
                          {!isLast && (
                            <div className="w-0.5 flex-1 bg-border my-1 min-h-[20px]" />
                          )}
                        </div>
                        <div className="flex-1 pb-4">
                          <p
                            className="font-medium capitalize"
                            style={{ fontFamily: '"Dream Avenue"' }}
                          >
                            {event.status.replace("-", " ")}
                          </p>
                          {event.notes && (
                            <p
                              className="text-sm text-muted-foreground"
                              style={{ fontFamily: '"Dream Avenue"' }}
                            >
                              {event.notes}
                            </p>
                          )}
                          <p
                            className="text-xs text-muted-foreground mt-1"
                            style={{ fontFamily: '"Dream Avenue"' }}
                          >
                            {event.location && `${event.location} • `}
                            {new Date(event.timestamp).toLocaleDateString(
                              "en-US",
                              {
                                month: "short",
                                day: "numeric",
                                hour: "numeric",
                                minute: "2-digit",
                              }
                            )}
                            {event.updatedByUserName &&
                              ` • Updated by ${event.updatedByUserName}`}
                          </p>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </div>
        )}

        {!order && !error && (
          <div className="text-center py-12 border border-border">
            <Package className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
            <p
              className="text-muted-foreground"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              Enter an order ID above to track your order
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
