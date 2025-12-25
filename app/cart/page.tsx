"use client"

import { Label } from "@/components/ui/label"

import { Header } from "@/components/header"
import { Button } from "@/components/ui/button"
import { useCart } from "@/components/cart-provider"
import { useAuth } from "@/components/auth-provider"
import Image from "next/image"
import Link from "next/link"
import { Minus, Plus, X, Tag, ArrowLeft } from "lucide-react"
import { useState } from "react"
import { Input } from "@/components/ui/input"
import { useToast } from "@/hooks/use-toast"
import { api } from "@/lib/api-client"

export default function CartPage() {
  const { items, removeItem, updateQuantity, total, promoCode, setPromoCode } = useCart()
  const { user, userId, isAuthenticated } = useAuth()
  const [couponCode, setCouponCode] = useState("")
  const [isApplying, setIsApplying] = useState(false)
  const { toast } = useToast()

  const handleApplyCoupon = async () => {
    if (!couponCode.trim()) {
      toast({
        title: "Invalid coupon",
        description: "Please enter a coupon code",
        variant: "destructive",
      })
      return
    }

    setIsApplying(true)
    try {
      // Get product IDs from cart items
      const productIds = items
        .filter(item => item.productId)
        .map(item => item.productId!)
      
      // Validate promocode with API
      const validation = await api.coupons.validate(
        couponCode.trim().toUpperCase(),
        total,
        productIds.length > 0 ? productIds : undefined,
        userId || undefined
      ) as any

      if (validation.valid) {
        setPromoCode({
          code: validation.code,
          discountType: validation.discountType,
          discountValue: validation.discountValue,
          discountAmount: validation.discountAmount,
        })
        toast({
          title: "Promo code applied!",
          description: `You saved ${validation.discountAmount.toFixed(2)} EGP on your order`,
        })
        setCouponCode("")
      } else {
        toast({
          title: "Invalid promo code",
          description: validation.message || "Please check your promo code and try again",
          variant: "destructive",
        })
      }
    } catch (error: any) {
      console.error("Error validating promo code:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to validate promo code. Please try again.",
        variant: "destructive",
      })
    } finally {
      setIsApplying(false)
    }
  }

  const handleRemoveCoupon = () => {
    setPromoCode(null)
    setCouponCode("")
  }

  const discountAmount = promoCode ? promoCode.discountAmount : 0
  const subtotalAfterDiscount = total - discountAmount

  if (items.length === 0) {
    return (
      <div className="min-h-screen">
        <Header />
        <div className="container mx-auto px-4 md:px-6 pt-24 sm:pt-28 md:pt-32 pb-16 md:pb-24">
          <div className="max-w-2xl mx-auto text-center">
            <Button variant="ghost" asChild className="mb-6" style={{ backgroundColor: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
              <Link href="/shop">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Back to Shop
              </Link>
            </Button>
            <h1 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Your Cart</h1>
            <p className="text-muted-foreground mb-8" style={{ fontFamily: '"Dream Avenue"' }}>Your shopping cart is empty</p>
            <Button asChild size="lg" style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
              <Link href="/shop">Continue Shopping</Link>
            </Button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8 md:py-12 pt-40 mt-12">
        <div className="mt-8">
          <Button variant="ghost" asChild className="mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
            <Link href="/shop">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Continue Shopping
            </Link>
          </Button>
        </div>

        <h1 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-6 sm:mb-8" style={{ fontFamily: '"Dream Avenue"' }}>Shopping Cart</h1>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 sm:gap-8 lg:gap-12">
          {/* Cart Items */}
          <div className="lg:col-span-2 space-y-4 sm:space-y-6">
            {items.map((item) => (
              <div key={`${item.id}-${item.size}`} className="flex gap-3 sm:gap-4 pb-4 sm:pb-6 border-b border-border">
                {/* Product Image */}
                <div className="relative w-20 h-24 sm:w-24 sm:h-32 bg-secondary flex-shrink-0 overflow-hidden">
                  {item.image && item.image.startsWith('data:image') ? (
                    // Use regular img tag for base64 images
                    <img 
                      src={item.image} 
                      alt={item.name} 
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <Image 
                      src={item.image || "/placeholder.svg"} 
                      alt={item.name} 
                      fill 
                      className="object-cover"
                      unoptimized={item.image?.startsWith('http') ? false : true}
                    />
                  )}
                </div>

                {/* Product Details */}
                <div className="flex-1 min-w-0">
                  <div className="flex justify-between gap-2 sm:gap-4 mb-2">
                    <div className="flex-1 min-w-0">
                      <h3 className="font-medium text-sm sm:text-base mb-1 truncate" style={{ fontFamily: '"Dream Avenue"' }}>{item.name}</h3>
                      <p className="text-xs sm:text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Size: {item.size}</p>
                    </div>
                    <button
                      onClick={() => removeItem(item.id, item.size)}
                      className="text-muted-foreground hover:text-foreground transition-colors flex-shrink-0"
                    >
                      <X className="h-4 w-4 sm:h-5 sm:w-5" />
                      <span className="sr-only">Remove item</span>
                    </button>
                  </div>

                  <div className="flex items-center justify-between mt-3 sm:mt-4">
                    {/* Quantity Controls */}
                    <div className="flex items-center gap-2 sm:gap-3 border border-border">
                      <button
                        onClick={() => updateQuantity(item.id, item.size, item.quantity - 1)}
                        className="px-2 py-1.5 sm:px-3 sm:py-2 hover:bg-secondary transition-colors"
                      >
                        <Minus className="h-3 w-3 sm:h-4 sm:w-4" />
                        <span className="sr-only">Decrease quantity</span>
                      </button>
                      <span className="w-6 sm:w-8 text-center text-sm sm:text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>{item.quantity}</span>
                      <button
                        onClick={() => updateQuantity(item.id, item.size, item.quantity + 1)}
                        className="px-2 py-1.5 sm:px-3 sm:py-2 hover:bg-secondary transition-colors"
                      >
                        <Plus className="h-3 w-3 sm:h-4 sm:w-4" />
                        <span className="sr-only">Increase quantity</span>
                      </button>
                    </div>

                    {/* Price */}
                    <p className="font-medium text-sm sm:text-base" style={{ fontFamily: '"Dream Avenue"' }}>{(item.price * item.quantity).toFixed(2)} EGP</p>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Order Summary */}
          <div className="lg:col-span-1">
            <div className="border border-border p-4 sm:p-6 lg:sticky lg:top-24 rounded-none">
              <h2 className="font-serif text-xl sm:text-2xl font-semibold mb-4 sm:mb-6" style={{ fontFamily: '"Dream Avenue"' }}>Order Summary</h2>

              <div className="mb-4 sm:mb-6 pb-4 sm:pb-6 border-b border-border">
                <Label htmlFor="coupon" className="text-sm font-medium mb-2 block" style={{ fontFamily: '"Dream Avenue"' }}>
                  Promo Code
                </Label>
                {promoCode ? (
                  <div className="flex items-center justify-between p-3 bg-secondary border border-border">
                    <div className="flex items-center gap-2">
                      <Tag className="h-4 w-4 text-green-600" />
                      <span className="text-sm font-medium" style={{ fontFamily: '"Dream Avenue"' }}>{promoCode.code}</span>
                      <span className="text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                        {promoCode.discountType === "Percentage" 
                          ? `-${promoCode.discountValue}%` 
                          : `-${promoCode.discountValue.toFixed(2)} EGP`}
                      </span>
                    </div>
                    <button
                      onClick={handleRemoveCoupon}
                      className="text-xs text-muted-foreground hover:text-foreground"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      Remove
                    </button>
                  </div>
                ) : (
                  <div className="space-y-2">
                    <div className="flex gap-2">
                      <Input
                        id="coupon"
                        placeholder={isAuthenticated ? "Enter promo code" : "Sign in to use promo codes"}
                        value={couponCode}
                        onChange={(e) => setCouponCode(e.target.value)}
                        onKeyPress={(e) => {
                          if (e.key === "Enter" && isAuthenticated) {
                            handleApplyCoupon()
                          }
                        }}
                        disabled={!isAuthenticated}
                        className="flex-1"
                      />
                      <Button
                        type="button"
                        variant="outline"
                        onClick={handleApplyCoupon}
                        disabled={!isAuthenticated || !couponCode.trim() || isApplying}
                        className="px-4 bg-transparent"
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {isApplying ? "..." : "Apply"}
                      </Button>
                    </div>
                    {!isAuthenticated && (
                      <p className="text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                        To use promo codes, please{" "}
                        <Link href="/login" className="text-[#3D0811] underline hover:no-underline">
                          sign in
                        </Link>
                        {" "}or{" "}
                        <Link href="/signup" className="text-[#3D0811] underline hover:no-underline">
                          sign up
                        </Link>
                      </p>
                    )}
                  </div>
                )}
              </div>

              <div className="space-y-2 sm:space-y-3 mb-4 sm:mb-6">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Subtotal</span>
                  <span className="font-medium" style={{ fontFamily: '"Dream Avenue"' }}>{total.toFixed(2)} EGP</span>
                </div>
                {promoCode && (
                  <div className="flex justify-between text-sm text-green-600">
                    <span style={{ fontFamily: '"Dream Avenue"' }}>
                      Discount ({promoCode.code})
                    </span>
                    <span className="font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                      -{discountAmount.toFixed(2)} EGP
                    </span>
                  </div>
                )}
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Shipping</span>
                  <span className="font-medium" style={{ fontFamily: '"Dream Avenue"' }}>{subtotalAfterDiscount >= 3000 ? "Free" : "120.00 EGP"}</span>
                </div>
              </div>

              <div className="border-t border-border pt-3 sm:pt-4 mb-4 sm:mb-6">
                <div className="flex justify-between font-serif text-base sm:text-lg font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>
                  <span>Total</span>
                  <span>
                    {(
                      subtotalAfterDiscount +
                      (subtotalAfterDiscount >= 3000 ? 0 : 120)
                    ).toFixed(2)} EGP
                  </span>
                </div>
              </div>

              {subtotalAfterDiscount < 3000 && (
                <p className="text-xs text-muted-foreground mb-4 sm:mb-6" style={{ fontFamily: '"Dream Avenue"' }}>
                  Add {(3000 - subtotalAfterDiscount).toFixed(2)} EGP more for free shipping
                </p>
              )}

              <Button asChild size="lg" className="w-full h-11 sm:h-12 text-sm sm:text-base mb-3" style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                <Link href="/checkout">Proceed to Checkout</Link>
              </Button>

              <Button
                asChild
                variant="outline"
                size="lg"
                className="w-full h-11 sm:h-12 text-sm sm:text-base border-2 hover:bg-[#3D0811]/10"
                style={{ fontFamily: '"Dream Avenue"', borderColor: '#3D0811', color: '#3D0811' }}
              >
                <Link href="/shop">Continue Shopping</Link>
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
