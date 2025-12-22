"use client"

import type React from "react"

import Link from "next/link"
import Image from "next/image"
import { ShoppingBag } from "lucide-react"
import { Button } from "./ui/button"
import { useCart } from "./cart-provider"
import { useToast } from "@/hooks/use-toast"
import { useState } from "react"

interface Product {
  id: string
  name: string
  price: number
  image: string
  category: string
}

export function ProductCard({ product }: { product: Product }) {
  const { addItem } = useCart()
  const { toast } = useToast()
  const [isAdding, setIsAdding] = useState(false)
  const [showCartAnimation, setShowCartAnimation] = useState(false)

  const handleQuickAdd = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsAdding(true)
    setShowCartAnimation(true)

    // Optimize image - if it's a large base64, use placeholder
    let optimizedImage = product.image || '/placeholder.svg'
    if (optimizedImage.startsWith('data:image') && optimizedImage.length > 67000) {
      optimizedImage = '/placeholder.svg'
    }

    addItem({
      id: product.id,
      name: product.name,
      price: product.price,
      image: optimizedImage,
      size: "M", // Default size for quick add
    })

    toast({
      title: "Added to cart",
      description: `${product.name} - Size M (default)`,
      duration: 2000,
    })

    setTimeout(() => {
      setIsAdding(false)
      setShowCartAnimation(false)
    }, 2000)
  }

  return (
    <div className="group relative">
      <Link href={`/product/${product.id}`}>
        <div className="relative aspect-[3/4] overflow-hidden bg-secondary mb-4">
          {product.image && product.image.startsWith('data:image') ? (
            // Use regular img tag for base64 images
            <img 
              src={product.image} 
              alt={product.name} 
              className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
            />
          ) : (
            <Image
              src={product.image || "/placeholder.svg"}
              alt={product.name}
              fill
              className="object-cover transition-transform duration-500 group-hover:scale-105"
              unoptimized={product.image?.startsWith('http://localhost') || product.image?.startsWith('data:')}
            />
          )}
          <div className="absolute inset-0 bg-black/0 group-hover:bg-black/10 transition-colors duration-300" />

          {showCartAnimation && (
            <div className="absolute top-4 right-4 animate-in zoom-in-50 fade-in duration-300">
              <div className="bg-foreground text-background rounded-full p-2 animate-bounce">
                <ShoppingBag className="h-4 w-4" />
              </div>
            </div>
          )}

          <Button
            onClick={handleQuickAdd}
            disabled={isAdding}
            className="absolute bottom-4 left-1/2 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition-all duration-300 hover:scale-105 disabled:scale-100"
            size="sm"
            style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
          >
            <ShoppingBag className="h-4 w-4 mr-2" />
            {isAdding ? "Added!" : "Quick Add"}
          </Button>
        </div>
        <div className="space-y-1">
          <p className="text-xs text-muted-foreground uppercase tracking-wider">{product.category}</p>
          <h3 className="font-medium text-sm">{product.name}</h3>
          <p className="text-sm font-medium">{product.price.toFixed(2)} EGP</p>
        </div>
      </Link>
    </div>
  )
}
