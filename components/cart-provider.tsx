"use client"

import type React from "react"

import { createContext, useContext, useState, useEffect, useCallback } from "react"
import { useToast } from "@/hooks/use-toast"
import { api } from "@/lib/api-client"
import { useAuth } from "./auth-provider"

export interface CartItem {
  id: string
  productId?: number // Product ID from backend
  variantId?: number // Variant ID from backend (for inventory deduction)
  name: string
  price: number
  image: string
  size: string
  quantity: number
  cartItemId?: number // Backend cart item ID (for authenticated users)
}

interface PromoCodeInfo {
  code: string
  discountType: string
  discountValue: number
  discountAmount: number
}

interface CartContextType {
  items: CartItem[]
  addItem: (item: Omit<CartItem, "quantity">) => Promise<void>
  removeItem: (id: string, size: string) => Promise<void>
  updateQuantity: (id: string, size: string, quantity: number) => Promise<void>
  clearCart: () => Promise<void>
  total: number
  itemCount: number
  isLoading: boolean
  syncWithBackend: () => Promise<void>
  promoCode: PromoCodeInfo | null
  setPromoCode: (promoCode: PromoCodeInfo | null) => void
}

const CartContext = createContext<CartContextType | undefined>(undefined)

// Backend cart item response structure
interface BackendCartItem {
  id: number
  productId: number
  variantId?: number | null // Variant ID if item has a variant
  productName: string
  productSKU?: string
  productDescription?: string
  quantity: number
  unitPrice: number
  totalPrice: number
  unit?: string
  categoryName?: string
  availableQuantity: number
  createdAt: string
  updatedAt: string
}

interface BackendCartResponse {
  items: BackendCartItem[]
  subTotal: number
  tax: number
  total: number
  itemCount: number
}

export function CartProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<CartItem[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [promoCode, setPromoCodeState] = useState<PromoCodeInfo | null>(null)
  const { toast } = useToast()
  const { isAuthenticated, userId } = useAuth()

  // Load promocode from localStorage on mount
  useEffect(() => {
    try {
      const saved = localStorage.getItem("promoCode")
      if (saved) {
        setPromoCodeState(JSON.parse(saved))
      }
    } catch (error) {
      console.error("Error loading promo code from localStorage:", error)
    }
  }, [])

  // Save promocode to localStorage when it changes
  useEffect(() => {
    if (promoCode) {
      try {
        localStorage.setItem("promoCode", JSON.stringify(promoCode))
      } catch (error) {
        console.error("Error saving promo code to localStorage:", error)
      }
    } else {
      try {
        localStorage.removeItem("promoCode")
      } catch (error) {
        console.error("Error removing promo code from localStorage:", error)
      }
    }
  }, [promoCode])

  const setPromoCode = (code: PromoCodeInfo | null) => {
    setPromoCodeState(code)
  }

  // Helper function to optimize image data (remove base64 if too large)
  const optimizeImageForStorage = (image: string): string => {
    if (image.startsWith('data:image')) {
      const base64Length = image.length
      if (base64Length > 133000) {
        return '/placeholder.svg'
      }
    }
    return image
  }

  // Map backend cart item to frontend cart item
  const mapBackendToFrontend = async (backendItem: BackendCartItem): Promise<CartItem> => {
    // Try to get size from localStorage mapping (productId -> size)
    // This preserves size information that was stored when adding to cart
    let size = "Default"
    try {
      const sizeMapping = localStorage.getItem("cartSizeMapping")
      if (sizeMapping) {
        const mapping = JSON.parse(sizeMapping)
        // Try to find size by cartItemId first, then by productId
        const sizeByCartItem = mapping[`cartItem_${backendItem.id}`]
        const sizeByProduct = mapping[`product_${backendItem.productId}`]
        size = sizeByCartItem || sizeByProduct || "Default"
      }
    } catch (error) {
      console.error("Error reading size mapping from localStorage:", error)
    }
    
    let image = '/placeholder.svg'
    
    try {
      const product = await api.products.getById(backendItem.productId.toString())
      if (product && typeof product === 'object' && 'mediaUrls' in product) {
        const mediaUrls = (product as any).mediaUrls
        if (mediaUrls) {
          try {
            const urls = typeof mediaUrls === 'string' ? JSON.parse(mediaUrls) : mediaUrls
            if (Array.isArray(urls) && urls.length > 0) {
              image = urls[0]
            } else if (typeof urls === 'string') {
              image = urls
            }
          } catch {
            if (typeof mediaUrls === 'string') {
              image = mediaUrls
            }
          }
        } else if ((product as any).imageUrl) {
          image = (product as any).imageUrl
        }
      }
    } catch (error) {
      console.error("Error fetching product details for cart item:", error)
    }

    return {
      id: backendItem.productId.toString(),
      productId: backendItem.productId,
      variantId: backendItem.variantId ?? undefined,
      name: backendItem.productName,
      price: Number(backendItem.unitPrice),
      image: optimizeImageForStorage(image),
      size: size,
      quantity: Number(backendItem.quantity),
      cartItemId: backendItem.id,
    }
  }

  // Load cart from backend (authenticated users)
  const loadCartFromBackend = useCallback(async () => {
    if (!isAuthenticated) {
      return
    }

    setIsLoading(true)
    try {
      const response = await api.cart.get() as BackendCartResponse
      
      if (response && response.items) {
        const mappedItems = await Promise.all(
          response.items.map(item => mapBackendToFrontend(item))
        )
        setItems(mappedItems)
        
        // Also save to localStorage as backup
        try {
          localStorage.setItem("cart", JSON.stringify(mappedItems))
        } catch (error) {
          console.error("Error saving cart to localStorage:", error)
        }
      } else {
        setItems([])
      }
    } catch (error: any) {
      console.error("Error loading cart from backend:", error)
      
      // If backend fails, try to load from localStorage as fallback
      try {
        const saved = localStorage.getItem("cart")
        if (saved) {
          const parsed = JSON.parse(saved)
          const optimized = parsed.map((item: CartItem) => ({
            ...item,
            image: optimizeImageForStorage(item.image)
          }))
          setItems(optimized)
        }
      } catch (localError) {
        console.error("Error loading cart from localStorage:", localError)
      }
      
      // Show error toast only if it's not a 401/403 (unauthorized)
      if (error.status !== 401 && error.status !== 403) {
        toast({
          title: "Error loading cart",
          description: "Could not load your cart. Using local storage.",
          variant: "destructive",
        })
      }
    } finally {
      setIsLoading(false)
    }
  }, [isAuthenticated, toast])

  // Load cart from localStorage (unauthenticated users)
  const loadCartFromLocalStorage = useCallback(() => {
    if (isAuthenticated) {
      return // Don't load from localStorage if authenticated
    }

    try {
      const saved = localStorage.getItem("cart")
      if (saved) {
        const parsed = JSON.parse(saved)
        const optimized = parsed.map((item: CartItem) => ({
          ...item,
          image: optimizeImageForStorage(item.image)
        }))
        setItems(optimized)
      }
    } catch (error) {
      console.error("Error loading cart from localStorage:", error)
      localStorage.removeItem("cart")
    }
  }, [isAuthenticated])

  // Initial load
  useEffect(() => {
    if (isAuthenticated) {
      loadCartFromBackend()
    } else {
      loadCartFromLocalStorage()
    }
  }, [isAuthenticated, loadCartFromBackend, loadCartFromLocalStorage])

  // Sync with backend (manual sync function)
  const syncWithBackend = useCallback(async () => {
    if (isAuthenticated) {
      await loadCartFromBackend()
    }
  }, [isAuthenticated, loadCartFromBackend])

  // Save cart to localStorage (for unauthenticated users or as backup)
  useEffect(() => {
    if (!isAuthenticated) {
      try {
        const optimizedItems = items.map(item => ({
          ...item,
          image: optimizeImageForStorage(item.image)
        }))
        
        const cartData = JSON.stringify(optimizedItems)
        
        if (cartData.length > 4000000) {
          console.warn("Cart data is too large, clearing old items")
          const recentItems = optimizedItems.slice(-10)
          localStorage.setItem("cart", JSON.stringify(recentItems))
          setItems(recentItems)
          return
        }
        
        localStorage.setItem("cart", cartData)
      } catch (error: any) {
        if (error.name === 'QuotaExceededError' || error.code === 22) {
          console.error("localStorage quota exceeded, clearing cart")
          setItems([])
          try {
            localStorage.removeItem("cart")
          } catch {
            // Ignore errors when clearing
          }
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
    } else {
      // For authenticated users, also save to localStorage as backup
      try {
        const optimizedItems = items.map(item => ({
          ...item,
          image: optimizeImageForStorage(item.image)
        }))
        localStorage.setItem("cart", JSON.stringify(optimizedItems))
      } catch (error) {
        console.error("Error saving cart backup to localStorage:", error)
      }
    }
  }, [items, isAuthenticated, toast])

  const addItem = async (item: Omit<CartItem, "quantity">) => {
    // Store size mapping in localStorage for both authenticated and unauthenticated users
    try {
      const sizeMapping = JSON.parse(localStorage.getItem("cartSizeMapping") || "{}")
      // Store by productId for now (we'll update with cartItemId after backend response)
      sizeMapping[`product_${item.productId || item.id}`] = item.size
      localStorage.setItem("cartSizeMapping", JSON.stringify(sizeMapping))
    } catch (error) {
      console.error("Error storing size mapping:", error)
    }

    if (isAuthenticated && item.productId) {
      // Add to backend
      setIsLoading(true)
      try {
        const response = await api.cart.addItem({
          productId: item.productId,
          variantId: item.variantId,
          quantity: 1,
        }) as any
        
        // Update size mapping with cartItemId if available
        if (response && response.id) {
          try {
            const sizeMapping = JSON.parse(localStorage.getItem("cartSizeMapping") || "{}")
            sizeMapping[`cartItem_${response.id}`] = item.size
            // Also keep productId mapping as fallback
            sizeMapping[`product_${item.productId}`] = item.size
            localStorage.setItem("cartSizeMapping", JSON.stringify(sizeMapping))
          } catch (error) {
            console.error("Error updating size mapping with cartItemId:", error)
          }
        }
        
        // Reload cart from backend
        await loadCartFromBackend()
        
        toast({
          title: "Added to cart!",
          description: `${item.name} has been added to your cart`,
          duration: 2000,
        })
      } catch (error: any) {
        console.error("Error adding item to cart:", error)
        
        // Fallback to local storage
        setItems((prev) => {
          const existing = prev.find((i) => i.id === item.id && i.size === item.size)
          if (existing) {
            return prev.map((i) => (i.id === item.id && i.size === item.size ? { ...i, quantity: i.quantity + 1 } : i))
          }
          return [...prev, { ...item, quantity: 1 }]
        })
        
        toast({
          title: "Added to cart (local)",
          description: "Item added locally. Please log in to sync with your account.",
          variant: "default",
          duration: 3000,
        })
      } finally {
        setIsLoading(false)
      }
    } else {
      // Add to local storage only
      setItems((prev) => {
        const existing = prev.find((i) => i.id === item.id && i.size === item.size)
        if (existing) {
          return prev.map((i) => (i.id === item.id && i.size === item.size ? { ...i, quantity: i.quantity + 1 } : i))
        }
        return [...prev, { ...item, quantity: 1 }]
      })
    }
  }

  const removeItem = async (id: string, size: string) => {
    const itemToRemove = items.find((i) => i.id === id && i.size === size)
    
    // Clean up size mapping from localStorage
    if (itemToRemove) {
      try {
        const sizeMapping = JSON.parse(localStorage.getItem("cartSizeMapping") || "{}")
        if (itemToRemove.cartItemId) {
          delete sizeMapping[`cartItem_${itemToRemove.cartItemId}`]
        }
        if (itemToRemove.productId) {
          delete sizeMapping[`product_${itemToRemove.productId}`]
        }
        localStorage.setItem("cartSizeMapping", JSON.stringify(sizeMapping))
      } catch (error) {
        console.error("Error cleaning up size mapping:", error)
      }
    }
    
    if (isAuthenticated && itemToRemove?.cartItemId) {
      // Remove from backend
      setIsLoading(true)
      try {
        await api.cart.removeItem(itemToRemove.cartItemId)
        
        // Reload cart from backend
        await loadCartFromBackend()
        
        toast({
          title: "Item removed",
          description: "Item has been removed from your cart",
          duration: 2000,
        })
      } catch (error: any) {
        console.error("Error removing item from cart:", error)
        
        // Fallback to local removal
        setItems((prev) => prev.filter((i) => !(i.id === id && i.size === size)))
        
        toast({
          title: "Item removed (local)",
          description: "Item removed locally. Please log in to sync with your account.",
          variant: "default",
          duration: 3000,
        })
      } finally {
        setIsLoading(false)
      }
    } else {
      // Remove from local storage only
      setItems((prev) => prev.filter((i) => !(i.id === id && i.size === size)))
    }
  }

  const updateQuantity = async (id: string, size: string, quantity: number) => {
    if (quantity <= 0) {
      await removeItem(id, size)
      return
    }

    const itemToUpdate = items.find((i) => i.id === id && i.size === size)
    
    if (isAuthenticated && itemToUpdate?.cartItemId) {
      // Update in backend
      setIsLoading(true)
      try {
        await api.cart.updateItem(itemToUpdate.cartItemId, {
          quantity: quantity,
        })
        
        // Reload cart from backend
        await loadCartFromBackend()
        
        toast({
          title: "Quantity updated",
          description: "Cart quantity has been updated",
          duration: 2000,
        })
      } catch (error: any) {
        console.error("Error updating cart item:", error)
        
        // Fallback to local update
        setItems((prev) => prev.map((i) => (i.id === id && i.size === size ? { ...i, quantity } : i)))
        
        toast({
          title: "Quantity updated (local)",
          description: "Quantity updated locally. Please log in to sync with your account.",
          variant: "default",
          duration: 3000,
        })
      } finally {
        setIsLoading(false)
      }
    } else {
      // Update in local storage only
      setItems((prev) => prev.map((i) => (i.id === id && i.size === size ? { ...i, quantity } : i)))
    }
  }

  const clearCart = async () => {
    if (isAuthenticated) {
      // Clear from backend
      setIsLoading(true)
      try {
        await api.cart.clear()
        
        setItems([])
        localStorage.removeItem("cart")
        setPromoCodeState(null)
        localStorage.removeItem("promoCode")
        
        toast({
          title: "Cart cleared",
          description: "Your cart has been cleared",
          duration: 2000,
        })
      } catch (error: any) {
        console.error("Error clearing cart:", error)
        
        // Fallback to local clear
        setItems([])
        setPromoCodeState(null)
        try {
          localStorage.removeItem("cart")
          localStorage.removeItem("promoCode")
        } catch (localError) {
          console.error("Error clearing cart from localStorage:", localError)
        }
        
        toast({
          title: "Cart cleared (local)",
          description: "Cart cleared locally. Please log in to sync with your account.",
          variant: "default",
          duration: 3000,
        })
      } finally {
        setIsLoading(false)
      }
    } else {
      // Clear from local storage only
      setItems([])
      setPromoCodeState(null)
      try {
        localStorage.removeItem("cart")
        localStorage.removeItem("promoCode")
      } catch (error) {
        console.error("Error clearing cart from localStorage:", error)
      }
    }
  }

  const total = items.reduce((sum, item) => sum + item.price * item.quantity, 0)
  const itemCount = items.reduce((sum, item) => sum + item.quantity, 0)

  return (
    <CartContext.Provider value={{ 
      items, 
      addItem, 
      removeItem, 
      updateQuantity, 
      clearCart, 
      total, 
      itemCount,
      isLoading,
      syncWithBackend,
      promoCode,
      setPromoCode,
    }}>
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
