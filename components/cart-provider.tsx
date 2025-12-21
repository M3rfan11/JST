"use client"

import type React from "react"

import { createContext, useContext, useState, useEffect } from "react"
import { useToast } from "@/hooks/use-toast"

export interface CartItem {
  id: string
  productId?: number // Product ID from backend
  name: string
  price: number
  image: string
  size: string
  quantity: number
}

interface CartContextType {
  items: CartItem[]
  addItem: (item: Omit<CartItem, "quantity">) => void
  removeItem: (id: string, size: string) => void
  updateQuantity: (id: string, size: string, quantity: number) => void
  clearCart: () => void
  total: number
  itemCount: number
}

const CartContext = createContext<CartContextType | undefined>(undefined)

export function CartProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<CartItem[]>([])
  const { toast } = useToast()

  // Helper function to optimize image data (remove base64 if too large)
  const optimizeImageForStorage = (image: string): string => {
    // If it's a base64 data URL and too large, convert to a placeholder
    if (image.startsWith('data:image')) {
      // Base64 images can be very large - only keep if under 100KB (increased threshold)
      const base64Length = image.length
      // Rough estimate: base64 is ~4/3 the size of binary, so 100KB binary ≈ 133KB base64
      // Increased threshold to preserve more images
      if (base64Length > 133000) {
        // Too large, return a placeholder
        return '/placeholder.svg'
      }
    }
    return image
  }

  // Load cart from localStorage on mount
  useEffect(() => {
    try {
      const saved = localStorage.getItem("cart")
      if (saved) {
        const parsed = JSON.parse(saved)
        // Optimize images when loading (in case old cart has large images)
        const optimized = parsed.map((item: CartItem) => ({
          ...item,
          image: optimizeImageForStorage(item.image)
        }))
        setItems(optimized)
      }
    } catch (error) {
      console.error("Error loading cart from localStorage:", error)
      // Clear corrupted cart data
      localStorage.removeItem("cart")
    }
  }, [])

  // Save cart to localStorage whenever it changes
  useEffect(() => {
    try {
      // Optimize images before saving
      const optimizedItems = items.map(item => ({
        ...item,
        image: optimizeImageForStorage(item.image)
      }))
      
      const cartData = JSON.stringify(optimizedItems)
      
      // Check size before saving (localStorage limit is typically 5-10MB)
      if (cartData.length > 4000000) { // 4MB threshold
        console.warn("Cart data is too large, clearing old items")
        // Keep only the most recent 10 items
        const recentItems = optimizedItems.slice(-10)
        localStorage.setItem("cart", JSON.stringify(recentItems))
        setItems(recentItems)
        return
      }
      
      localStorage.setItem("cart", cartData)
    } catch (error: any) {
      if (error.name === 'QuotaExceededError' || error.code === 22) {
        console.error("localStorage quota exceeded, clearing cart")
        // Clear the cart and notify user
        setItems([])
        try {
          localStorage.removeItem("cart")
        } catch {
          // Ignore errors when clearing
        }
        // Show toast notification
        toast({
          title: "Cart cleared",
          description: "Your cart was too large and has been cleared. Please add items again.",
          variant: "destructive",
          duration: 5000,
        })
      } else {
        console.error("Error saving cart to localStorage:", error)
      }
    }
  }, [items])

  const addItem = (item: Omit<CartItem, "quantity">) => {
    setItems((prev) => {
      const existing = prev.find((i) => i.id === item.id && i.size === item.size)
      if (existing) {
        return prev.map((i) => (i.id === item.id && i.size === item.size ? { ...i, quantity: i.quantity + 1 } : i))
      }
      return [...prev, { ...item, quantity: 1 }]
    })
  }

  const removeItem = (id: string, size: string) => {
    setItems((prev) => prev.filter((i) => !(i.id === id && i.size === size)))
  }

  const updateQuantity = (id: string, size: string, quantity: number) => {
    if (quantity <= 0) {
      removeItem(id, size)
      return
    }
    setItems((prev) => prev.map((i) => (i.id === id && i.size === size ? { ...i, quantity } : i)))
  }

  const clearCart = () => {
    setItems([])
    try {
      localStorage.removeItem("cart")
    } catch (error) {
      console.error("Error clearing cart from localStorage:", error)
    }
  }

  const total = items.reduce((sum, item) => sum + item.price * item.quantity, 0)
  const itemCount = items.reduce((sum, item) => sum + item.quantity, 0)

  return (
    <CartContext.Provider value={{ items, addItem, removeItem, updateQuantity, clearCart, total, itemCount }}>
      {children}
    </CartContext.Provider>
  )
}

export function useCart() {
  const context = useContext(CartContext)
  if (!context) {
    throw new Error("useCart must be used within CartProvider")
  }
  return context
}

