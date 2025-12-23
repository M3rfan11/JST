"use client";

import type React from "react";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import { useCart } from "./cart-provider";
import { useAuth } from "./auth-provider";
import { useToast } from "@/hooks/use-toast";
import { CreditCard, Lock } from "lucide-react";
import { api } from "@/lib/api-client";

interface CheckoutFormProps {
  total: number;
}

export function CheckoutForm({ total }: CheckoutFormProps) {
  const router = useRouter();
  const { items, clearCart, promoCode, setPromoCode } = useCart();
  const { user, isAuthenticated } = useAuth();

  // Clear promocode if user is not authenticated (guest users can't use promocodes)
  useEffect(() => {
    if (!isAuthenticated && promoCode) {
      setPromoCode(null);
    }
  }, [isAuthenticated, promoCode, setPromoCode]);
  const { toast } = useToast();
  const [isProcessing, setIsProcessing] = useState(false);

  const [formData, setFormData] = useState({
    // Contact
    email: user?.email || "",
    // Shipping
    firstName: user?.firstName || "",
    lastName: user?.lastName || "",
    address: "",
    apartment: "",
    city: "",
    country: "",
    state: "",
    zipCode: "",
    phone: "",
    // Payment
    cardNumber: "",
    cardName: "",
    expiryDate: "",
    cvv: "",
    paymentMethod: "Cash on Delivery", // Default payment method
  });

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsProcessing(true);

    try {
      // Build customer address
      const addressParts = [
        formData.address,
        formData.apartment && `Apt ${formData.apartment}`,
        formData.city,
        formData.state,
        formData.zipCode,
        formData.country,
      ].filter(Boolean);
      const fullAddress = addressParts.join(", ");

      // Build customer name
      const customerName = `${formData.firstName} ${formData.lastName}`.trim();

      // Map cart items to order items
      // Note: We need productId from the cart items. If not available, we'll need to fetch it.
      // For now, we'll try to extract it from the id if it's numeric, or fetch products
      const orderItems = items.map((item) => {
        // Try to get productId from item (if it was stored)
        const productId =
          item.productId ||
          (item.id && !isNaN(Number(item.id)) ? Number(item.id) : null);

        if (!productId) {
          throw new Error(`Product ID not found for item: ${item.name}`);
        }

        return {
          productId: productId,
          variantId: item.variantId, // Include variant ID for inventory deduction
          quantity: item.quantity,
          unitPrice: item.price,
          unit: "piece",
        };
      });

      if (orderItems.length === 0) {
        toast({
          title: "Error",
          description: "Your cart is empty.",
          variant: "destructive",
        });
        setIsProcessing(false);
        return;
      }

      // Prepare order request
      const orderRequest = {
        customerName: customerName,
        customerEmail: formData.email,
        customerPhone: formData.phone,
        customerAddress: fullAddress,
        items: orderItems,
        notes: `Payment Method: ${formData.paymentMethod}`,
        paymentMethod: formData.paymentMethod,
        promoCode: promoCode?.code || undefined, // Include promo code from cart if available
        useCartItems: false, // We're providing items directly
      };

      // Create order via API
      // Check if user has Customer role - if not, use guest endpoint
      const isCustomer = user?.roles?.includes("Customer") || false;
      let orderResponse: any;

      if (isAuthenticated && user && isCustomer) {
        // Authenticated Customer - use authenticated endpoint
        try {
          orderResponse = (await api.customerOrders.createOrder(
            orderRequest
          )) as any;
        } catch (error: any) {
          // If authenticated endpoint fails (e.g., SuperAdmin), fall back to guest
          if (error.status === 403) {
            orderResponse = (await api.customerOrders.createGuestOrder(
              orderRequest
            )) as any;
          } else {
            throw error;
          }
        }
      } else {
        // Guest user or non-Customer (like SuperAdmin) - use guest endpoint
        orderResponse = (await api.customerOrders.createGuestOrder(
          orderRequest
        )) as any;
      }

      // Clear cart and promo code
      clearCart();
      setPromoCode(null);

      // Show success message
      const orderNumber =
        orderResponse?.orderNumber || orderResponse?.order_number || "N/A";
      const orderId = orderResponse?.orderId || orderResponse?.id;

      // If InstaPay, redirect to payment pending page
      if (formData.paymentMethod === "InstaPay" && orderId) {
        router.push(`/orders/${orderId}/payment`);
        return;
      }

      toast({
        title: "Order placed successfully!",
        description: `Order #${orderNumber} has been created.`,
      });

      // Always redirect to track page with order number
      router.push(
        `/track?orderId=${orderNumber}&email=${encodeURIComponent(
          formData.email
        )}`
      );
    } catch (error: any) {
      console.error("Error creating order:", error);
      toast({
        title: "Error",
        description:
          error.message || "Failed to place order. Please try again.",
        variant: "destructive",
      });
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6 sm:space-y-8">
      {/* Contact Information */}
      <div>
        <h2
          className="font-serif text-xl sm:text-2xl font-semibold mb-3 sm:mb-4"
          style={{ fontFamily: '"Dream Avenue"' }}
        >
          Contact Information
        </h2>
        <div className="space-y-3 sm:space-y-4">
          <div>
            <Label
              htmlFor="email"
              className="text-sm"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              Email
            </Label>
            <Input
              id="email"
              name="email"
              type="email"
              required
              value={formData.email}
              onChange={handleChange}
              placeholder="you@example.com"
              className="mt-1.5"
            />
          </div>
        </div>
      </div>

      {/* Shipping Address */}
      <div>
        <h2
          className="font-serif text-xl sm:text-2xl font-semibold mb-3 sm:mb-4"
          style={{ fontFamily: '"Dream Avenue"' }}
        >
          Shipping Address
        </h2>
        <div className="space-y-3 sm:space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
            <div>
              <Label
                htmlFor="firstName"
                className="text-sm"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                First Name
              </Label>
              <Input
                id="firstName"
                name="firstName"
                required
                value={formData.firstName}
                onChange={handleChange}
                className="mt-1.5"
              />
            </div>
            <div>
              <Label
                htmlFor="lastName"
                className="text-sm"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                Last Name
              </Label>
              <Input
                id="lastName"
                name="lastName"
                required
                value={formData.lastName}
                onChange={handleChange}
                className="mt-1.5"
              />
            </div>
          </div>

          <div>
            <Label
              htmlFor="address"
              className="text-sm"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              Address
            </Label>
            <Input
              id="address"
              name="address"
              required
              value={formData.address}
              onChange={handleChange}
              placeholder="Street address"
              className="mt-1.5"
            />
          </div>

          <div>
            <Label
              htmlFor="apartment"
              className="text-sm"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              Apartment, suite, etc. (optional)
            </Label>
            <Input
              id="apartment"
              name="apartment"
              value={formData.apartment}
              onChange={handleChange}
              className="mt-1.5"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
            <div>
              <Label
                htmlFor="city"
                className="text-sm"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                City
              </Label>
              <Input
                id="city"
                name="city"
                required
                value={formData.city}
                onChange={handleChange}
                className="mt-1.5"
              />
            </div>
            <div>
              <Label
                htmlFor="state"
                className="text-sm"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                State / Province
              </Label>
              <Input
                id="state"
                name="state"
                required
                value={formData.state}
                onChange={handleChange}
                className="mt-1.5"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
            <div>
              <Label
                htmlFor="country"
                className="text-sm"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                Country
              </Label>
              <Input
                id="country"
                name="country"
                required
                value={formData.country}
                onChange={handleChange}
                className="mt-1.5"
              />
            </div>
            <div>
              <Label
                htmlFor="zipCode"
                className="text-sm"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                ZIP / Postal Code
              </Label>
              <Input
                id="zipCode"
                name="zipCode"
                required
                value={formData.zipCode}
                onChange={handleChange}
                className="mt-1.5"
              />
            </div>
          </div>

          <div>
            <Label
              htmlFor="phone"
              className="text-sm"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              Phone
            </Label>
            <Input
              id="phone"
              name="phone"
              type="tel"
              required
              value={formData.phone}
              onChange={handleChange}
              className="mt-1.5"
            />
          </div>
        </div>
      </div>

      {/* Payment Information */}
      <div>
        <h2
          className="font-serif text-xl sm:text-2xl font-semibold mb-3 sm:mb-4"
          style={{ fontFamily: '"Dream Avenue"' }}
        >
          Payment Information
        </h2>
        <div className="space-y-3 sm:space-y-4">
          <div>
            <Label
              htmlFor="paymentMethod"
              className="text-sm"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              Payment Method
            </Label>
            <select
              id="paymentMethod"
              name="paymentMethod"
              value={formData.paymentMethod}
              onChange={handleChange}
              className="mt-1.5 w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-[#3D0811]"
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              <option value="Cash on Delivery">Cash on Delivery</option>
              <option value="InstaPay">InstaPay</option>
              <option value="Credit Card">Credit Card</option>
              <option value="Bank Transfer">Bank Transfer</option>
            </select>
          </div>

          {formData.paymentMethod === "Credit Card" && (
            <>
              <div>
                <Label
                  htmlFor="cardNumber"
                  className="text-sm"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  Card Number
                </Label>
                <div className="relative mt-1.5">
                  <Input
                    id="cardNumber"
                    name="cardNumber"
                    required
                    value={formData.cardNumber}
                    onChange={handleChange}
                    placeholder="1234 5678 9012 3456"
                    maxLength={19}
                  />
                  <CreditCard className="absolute right-3 top-1/2 -translate-y-1/2 h-5 w-5 text-muted-foreground" />
                </div>
              </div>

              <div>
                <Label
                  htmlFor="cardName"
                  className="text-sm"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  Name on Card
                </Label>
                <Input
                  id="cardName"
                  name="cardName"
                  required={formData.paymentMethod === "Credit Card"}
                  value={formData.cardName}
                  onChange={handleChange}
                  className="mt-1.5"
                />
              </div>

              <div className="grid grid-cols-2 gap-3 sm:gap-4">
                <div>
                  <Label
                    htmlFor="expiryDate"
                    className="text-sm"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Expiry Date
                  </Label>
                  <Input
                    id="expiryDate"
                    name="expiryDate"
                    required={formData.paymentMethod === "Credit Card"}
                    value={formData.expiryDate}
                    onChange={handleChange}
                    placeholder="MM/YY"
                    maxLength={5}
                    className="mt-1.5"
                  />
                </div>
                <div>
                  <Label
                    htmlFor="cvv"
                    className="text-sm"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    CVV
                  </Label>
                  <Input
                    id="cvv"
                    name="cvv"
                    required={formData.paymentMethod === "Credit Card"}
                    value={formData.cvv}
                    onChange={handleChange}
                    placeholder="123"
                    maxLength={4}
                    className="mt-1.5"
                  />
                </div>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Submit Button */}
      <div className="border-t border-border pt-4 sm:pt-6">
        <Button
          type="submit"
          size="lg"
          className="w-full h-12 sm:h-14 text-sm sm:text-base"
          disabled={isProcessing}
          style={{
            backgroundColor: "rgba(61, 8, 17, 1)",
            color: "rgba(255, 255, 255, 1)",
            fontFamily: '"Dream Avenue"',
          }}
        >
          {isProcessing ? (
            <span className="flex items-center gap-2">
              <span className="animate-spin">⏳</span>
              Processing...
            </span>
          ) : (
            <span className="flex items-center gap-2">
              <Lock className="h-4 w-4 sm:h-5 sm:w-5" />
              Complete Order - ${total.toFixed(2)}
            </span>
          )}
        </Button>
        <p
          className="text-xs text-center text-muted-foreground mt-3"
          style={{ fontFamily: '"Dream Avenue"' }}
        >
          Your payment information is secure and encrypted
        </p>
      </div>
    </form>
  );
}
