"use client"

import { useState, useMemo } from "react"
import Image from "next/image"
import { Button } from "./ui/button"
import { useCart } from "./cart-provider"
import { useToast } from "@/hooks/use-toast"
import { Check, ShoppingBag, AlertCircle } from "lucide-react"

interface Product {
  id: string
  name: string
  price: number
  images: string[]
  category: string
  description: string
  details?: string[]
  sizes: string[]
  washingInstructions?: string
  variants?: any[]
}

export function ProductDetail({ product }: { product: Product }) {
  const [selectedImage, setSelectedImage] = useState(0)
  const [selectedSize, setSelectedSize] = useState<string>("")
  const [isAdding, setIsAdding] = useState(false)
  const [showSuccess, setShowSuccess] = useState(false)
  const [showSizeError, setShowSizeError] = useState(false)
  const { addItem } = useCart()
  const { toast } = useToast()

  // Get the current price based on selected size (variant price if available)
  // Use useMemo to ensure it updates reactively when selectedSize or variants change
  const currentPrice = useMemo(() => {
    // If no size is selected, return base product price
    if (!selectedSize) {
      console.log("No size selected, using base price:", product.price)
      return product.price
    }

    // If no variants available, return base product price
    if (!product.variants || !Array.isArray(product.variants) || product.variants.length === 0) {
      console.log("No variants available, using base price:", product.price)
      return product.price
    }

    console.log("Calculating price for selected size:", selectedSize)
    console.log("Available variants:", product.variants.map((v: any) => ({
      id: v.id,
      attributes: v.attributes,
      priceOverride: v.priceOverride || v.PriceOverride
    })))

    // Find the variant that matches the selected size
    const matchingVariant = product.variants.find((variant: any) => {
      if (!variant || !variant.attributes) {
        console.log("Variant missing or no attributes:", variant)
        return false
      }
      
      try {
        const attrs = typeof variant.attributes === 'string' 
          ? JSON.parse(variant.attributes) 
          : variant.attributes
        
        if (typeof attrs === 'object' && attrs !== null) {
          // Check for Size attribute in various formats
          const size = attrs['Size'] || attrs['size'] || attrs['SIZE'] || ''
          const matches = size === selectedSize
          console.log(`Variant size "${size}" matches selected "${selectedSize}":`, matches)
          return matches
        }
      } catch (error) {
        console.error("Error parsing variant attributes:", error, variant.attributes)
        return false
      }
      return false
    })

    console.log("Matching variant found:", matchingVariant)

    // If variant found and has priceOverride, use it
    if (matchingVariant) {
      // Check both camelCase and PascalCase (API might return either)
      const priceOverride = matchingVariant.priceOverride || matchingVariant.PriceOverride
      console.log("Variant priceOverride:", priceOverride)
      
      // If priceOverride exists and is greater than 0, use it
      if (priceOverride != null && priceOverride > 0) {
        console.log("Using variant price:", priceOverride)
        return Number(priceOverride)
      } else {
        console.log("Variant has no priceOverride, using base price:", product.price)
      }
    } else {
      console.log("No matching variant found, using base price:", product.price)
    }

    // Fallback to base product price
    return product.price
  }, [selectedSize, product.price, product.variants])

  const handleAddToCart = () => {
    if (!selectedSize) {
      setShowSizeError(true)
      toast({
        title: "Size Required",
        description: "Please select a size before adding to cart",
        variant: "destructive",
        duration: 3000,
      })
      setTimeout(() => setShowSizeError(false), 1000)
      return
    }

    setIsAdding(true)
    setShowSuccess(true)
    setShowSizeError(false)

    // Find the variant that matches the selected size
    let variantPrice = product.price
    let variantId: number | undefined = undefined

    if (product.variants && Array.isArray(product.variants)) {
      const matchingVariant = product.variants.find((variant: any) => {
        if (!variant.attributes) return false
        try {
          const attrs = typeof variant.attributes === 'string' 
            ? JSON.parse(variant.attributes) 
            : variant.attributes
          if (typeof attrs === 'object' && attrs !== null) {
            const size = attrs['Size'] || attrs['size'] || attrs['SIZE']
            return size === selectedSize
          }
        } catch {
          return false
        }
        return false
      })

      if (matchingVariant) {
        variantId = matchingVariant.id
        // Use variant's priceOverride if available, otherwise use product price
        if (matchingVariant.priceOverride && matchingVariant.priceOverride > 0) {
          variantPrice = matchingVariant.priceOverride
        }
      }
    }

    // Optimize image - if it's a large base64, use placeholder or first non-base64 image
    let optimizedImage = product.images[0] || '/placeholder.svg'
    if (optimizedImage.startsWith('data:image')) {
      // If base64 is too large (>50KB), use placeholder
      if (optimizedImage.length > 67000) {
        // Find first non-base64 image
        const nonBase64Image = product.images.find(img => !img.startsWith('data:image'))
        optimizedImage = nonBase64Image || '/placeholder.svg'
      }
    }

    const item = {
      id: product.id,
      productId: typeof product.id === 'string' && !isNaN(Number(product.id)) ? Number(product.id) : undefined,
      variantId: variantId, // Include variant ID for inventory tracking
      name: product.name,
      price: variantPrice,
      image: optimizedImage,
      size: selectedSize,
    }

    addItem(item)

    toast({
      title: "Added to cart!",
      description: `${product.name} - Size ${selectedSize}`,
      duration: 2000,
    })

    setTimeout(() => {
      setIsAdding(false)
      setShowSuccess(false)
    }, 2000)
  }

  return (
    <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8 md:py-12">
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 sm:gap-8 md:gap-12">
        {/* Product Images */}
        <div className="space-y-3 sm:space-y-4">
          <div className="relative aspect-[3/4] bg-secondary overflow-hidden">
            <Image
              src={product.images[selectedImage] || "/placeholder.svg"}
              alt={product.name}
              fill
              className="object-cover"
              priority
            />
            {showSuccess && (
              <div className="absolute inset-0 bg-foreground/90 flex items-center justify-center animate-in fade-in zoom-in-50 duration-300">
                <div className="bg-background text-foreground rounded-full p-4 sm:p-6 animate-in zoom-in-50 duration-500 delay-150">
                  <Check className="h-8 w-8 sm:h-12 sm:w-12" />
                </div>
              </div>
            )}
          </div>
          <div className="grid grid-cols-3 gap-2 sm:gap-3 md:gap-4">
            {product.images.map((image, index) => (
              <button
                key={index}
                onClick={() => setSelectedImage(index)}
                className={`relative aspect-[3/4] bg-secondary overflow-hidden border-2 transition-colors ${
                  selectedImage === index ? "border-foreground" : "border-transparent"
                }`}
              >
                <Image
                  src={image || "/placeholder.svg"}
                  alt={`${product.name} ${index + 1}`}
                  fill
                  className="object-cover"
                />
              </button>
            ))}
          </div>
        </div>

        {/* Product Info */}
        <div className="flex flex-col">
          <div className="mb-4 sm:mb-6">
            <p className="text-xs text-muted-foreground uppercase tracking-wider mb-2" style={{ fontFamily: '"Dream Avenue"' }}>{product.category}</p>
            <h1 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-3 sm:mb-4 text-balance" style={{ fontFamily: '"Dream Avenue"' }}>
              {product.name}
            </h1>
            <p className="text-xl sm:text-2xl font-medium" style={{ fontFamily: '"Dream Avenue"' }}>{currentPrice.toFixed(2)} EGP</p>
          </div>

          <p className="text-sm sm:text-base text-muted-foreground leading-relaxed mb-6 sm:mb-8" style={{ fontFamily: '"Dream Avenue"' }}>
            {product.description}
          </p>

          {/* Size Selection */}
          <div className={`mb-6 sm:mb-8 transition-all duration-300 ${showSizeError ? "animate-shake" : ""}`}>
            <div className="flex items-center justify-between mb-3 sm:mb-4">
              <h3 className="font-medium text-sm sm:text-base" style={{ fontFamily: '"Dream Avenue"' }}>Select Size</h3>
              {showSizeError && (
                <div className="flex items-center gap-1 text-destructive text-xs sm:text-sm animate-in fade-in slide-in-from-right-2" style={{ fontFamily: '"Dream Avenue"' }}>
                  <AlertCircle className="h-4 w-4" />
                  <span>Please select a size</span>
                </div>
              )}
            </div>
            <div
              className={`flex flex-wrap gap-2 p-3 border-2 rounded transition-colors ${
                showSizeError ? "border-destructive bg-destructive/5" : "border-transparent"
              }`}
            >
              {product.sizes.map((size) => (
                <button
                  key={size}
                  onClick={() => {
                    setSelectedSize(size)
                    setShowSizeError(false)
                  }}
                  className={`px-4 sm:px-6 py-2 sm:py-3 border transition-all duration-200 text-sm sm:text-base ${
                    selectedSize === size
                      ? "scale-105"
                      : "bg-background text-foreground border-border hover:border-foreground hover:scale-105"
                  }`}
                  style={{
                    fontFamily: '"Dream Avenue"',
                    ...(selectedSize === size
                      ? {
                          backgroundColor: 'rgba(206, 180, 157, 1)',
                          color: 'rgba(255, 255, 255, 1)',
                          borderColor: 'rgba(206, 180, 157, 1)',
                        }
                      : {}),
                  }}
                >
                  {size}
                </button>
              ))}
            </div>
          </div>

          <Button
            type="button"
            onClick={handleAddToCart}
            disabled={isAdding}
            size="lg"
            className="w-full h-12 sm:h-14 text-sm sm:text-base mb-6 sm:mb-8 transition-all duration-300 disabled:scale-100 hover:scale-[1.02] active:scale-[0.98]"
            style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
          >
            {isAdding ? (
              <>
                <Check className="h-5 w-5 mr-2 animate-in zoom-in-50 duration-300" />
                Added to Cart!
              </>
            ) : (
              <>
                <ShoppingBag className="h-5 w-5 mr-2" />
                Add to Cart
              </>
            )}
          </Button>

          {/* Product Details */}
          {product.details && product.details.length > 0 && (
            <div className="border-t border-border pt-6 sm:pt-8">
              <h3 className="font-medium text-sm sm:text-base mb-3 sm:mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Product Details</h3>
              <ul className="space-y-2">
                {product.details.map((detail, index) => (
                  <li key={index} className="flex items-start gap-2 text-xs sm:text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    <Check className="h-4 w-4 mt-0.5 flex-shrink-0" />
                    <span>{detail}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Washing Instructions */}
          {product.washingInstructions && (
            <div className={`border-t border-border ${product.details && product.details.length > 0 ? 'mt-6 sm:mt-8' : ''} pt-6 sm:pt-8`}>
              <h3 className="font-medium text-sm sm:text-base mb-3 sm:mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Washing Instructions</h3>
              <p className="text-xs sm:text-sm text-muted-foreground leading-relaxed whitespace-pre-line" style={{ fontFamily: '"Dream Avenue"' }}>
                {product.washingInstructions}
              </p>
            </div>
          )}

          {/* Shipping & Returns */}
          <div className="border-t border-border mt-6 sm:mt-8 pt-6 sm:pt-8 space-y-3 sm:space-y-4 text-xs sm:text-sm">
            <div>
              <h4 className="font-medium mb-2" style={{ fontFamily: '"Dream Avenue"' }}>Free Shipping</h4>
              <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Complimentary shipping on all orders over 3000 EGP</p>
            </div>
            <div>
              <h4 className="font-medium mb-2" style={{ fontFamily: '"Dream Avenue"' }}>Returns & Exchanges</h4>
              <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>30-day return policy for unworn items</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
