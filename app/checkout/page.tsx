"use client"

import { Header } from "@/components/header"
import { CheckoutForm } from "@/components/checkout-form"
import { useCart } from "@/components/cart-provider"
import { Button } from "@/components/ui/button"
import Link from "next/link"
import Image from "next/image"
import { ArrowLeft } from "lucide-react"

export default function CheckoutPage() {
  const { items, total, promoCode } = useCart()

  if (items.length === 0) {
    return (
      <div className="min-h-screen">
        <Header />
        <div className="container mx-auto px-4 md:px-6 py-16 md:py-24">
          <div className="max-w-2xl mx-auto text-center">
            <Button variant="ghost" asChild className="mb-6" style={{ fontFamily: '"Dream Avenue"' }}>
              <Link href="/shop">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Back to Shop
              </Link>
            </Button>
            <h1 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Checkout</h1>
            <p className="text-muted-foreground mb-8" style={{ fontFamily: '"Dream Avenue"' }}>Your cart is empty</p>
            <Button asChild size="lg" style={{ fontFamily: '"Dream Avenue"' }}>
              <Link href="/shop">Continue Shopping</Link>
            </Button>
          </div>
        </div>
      </div>
    )
  }

  const discountAmount = promoCode ? promoCode.discountAmount : 0
  const subtotalAfterDiscount = total - discountAmount
  const shipping = subtotalAfterDiscount >= 3000 ? 0 : 120
  const orderTotal = subtotalAfterDiscount + shipping

  return (
    <div className="min-h-screen bg-background">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8 md:py-12">
        <Button variant="ghost" asChild className="mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
          <Link href="/cart">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Cart
          </Link>
        </Button>

        <h1 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-6 sm:mb-8" style={{ fontFamily: '"Dream Avenue"' }}>Checkout</h1>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 sm:gap-8 lg:gap-12">
          {/* Checkout Form */}
          <div className="order-2 lg:order-1">
            <CheckoutForm total={orderTotal} />
          </div>

          {/* Order Summary */}
          <div className="order-1 lg:order-2">
            <div className="border border-border p-4 sm:p-6 lg:sticky lg:top-24 rounded-none">
              <h2 className="font-serif text-xl sm:text-2xl font-semibold mb-4 sm:mb-6" style={{ fontFamily: '"Dream Avenue"' }}>Order Summary</h2>

              {/* Cart Items */}
              <div className="space-y-3 sm:space-y-4 mb-4 sm:mb-6 max-h-48 sm:max-h-64 overflow-y-auto">
                {items.map((item) => (
                  <div key={`${item.id}-${item.size}`} className="flex gap-3">
                    <div className="relative w-14 sm:w-16 h-16 sm:h-20 bg-secondary flex-shrink-0 overflow-hidden">
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
                    <div className="flex-1 min-w-0">
                      <h3 className="text-sm font-medium mb-1 truncate" style={{ fontFamily: '"Dream Avenue"' }}>{item.name}</h3>
                      <p className="text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Size: {item.size}</p>
                      <p className="text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Qty: {item.quantity}</p>
                      <p className="text-sm font-medium mt-1" style={{ fontFamily: '"Dream Avenue"' }}>{(item.price * item.quantity).toFixed(2)} EGP</p>
                    </div>
                  </div>
                ))}
              </div>

              {/* Pricing */}
              <div className="border-t border-border pt-3 sm:pt-4 space-y-2 sm:space-y-3 mb-4 sm:mb-6">
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
                  <span className="font-medium" style={{ fontFamily: '"Dream Avenue"' }}>{shipping === 0 ? "Free" : `${shipping.toFixed(2)} EGP`}</span>
                </div>
              </div>

              <div className="border-t border-border pt-3 sm:pt-4">
                <div className="flex justify-between font-serif text-lg sm:text-xl font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>
                  <span>Total</span>
                  <span>{orderTotal.toFixed(2)} EGP</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
