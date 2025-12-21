"use client"

import { useState, useEffect } from "react"
import { ProductCard } from "./product-card"
import { api } from "@/lib/api-client"

interface Product {
  id: string
  name: string
  price: number
  image: string
  category: string
}

export function ProductGrid({ limit, categoryId }: { limit?: number; categoryId?: number }) {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadProducts()
  }, [categoryId])

  const loadProducts = async () => {
    try {
      setLoading(true)
      setError(null)
      
      // Fetch products from API (using public endpoint that doesn't require auth)
      const response = await api.products.getPublic() as any
      
      // Handle different response formats
      let productsList: any[] = []
      if (Array.isArray(response)) {
        productsList = response
      } else if (response && Array.isArray(response.value)) {
        productsList = response.value
      } else if (response && response.data && Array.isArray(response.data)) {
        productsList = response.data
      }
      
      // Filter by category if categoryId is provided
      if (categoryId) {
        productsList = productsList.filter((p: any) => {
          if (p.categoryId === categoryId) return true
          if (p.categoryIds && Array.isArray(p.categoryIds)) {
            return p.categoryIds.includes(categoryId)
          }
          return false
        })
      }
      
      // Map API response to ProductCard format
      const mappedProducts: Product[] = productsList
        .filter((p: any) => p.isActive !== false && (p.status === 1 || p.status === 'Active' || p.status === undefined))
        .map((p: any) => {
          // Get first image from mediaUrls or use imageUrl
          let imageUrl = "/placeholder.svg"
          if (p.mediaUrls) {
            try {
              const mediaUrls = typeof p.mediaUrls === 'string' ? JSON.parse(p.mediaUrls) : p.mediaUrls
              if (Array.isArray(mediaUrls) && mediaUrls.length > 0) {
                imageUrl = mediaUrls[0]
              }
            } catch {
              // If parsing fails, try imageUrl
              if (p.imageUrl) {
                imageUrl = p.imageUrl
              }
            }
          } else if (p.imageUrl) {
            imageUrl = p.imageUrl
          }
          
          // Get category name (first category if multiple)
          let categoryName = "Uncategorized"
          if (p.categoryName) {
            categoryName = p.categoryName
          } else if (p.categoryNames && Array.isArray(p.categoryNames) && p.categoryNames.length > 0) {
            categoryName = p.categoryNames[0]
          } else if (p.category && p.category.name) {
            categoryName = p.category.name
          }
          
          return {
            id: String(p.id),
            name: p.name || "Unnamed Product",
            price: p.price || 0,
            image: imageUrl,
            category: categoryName,
          }
        })
      
      setProducts(mappedProducts)
    } catch (err: any) {
      console.error("Error loading products:", err)
      setError(err.message || "Failed to load products")
      setProducts([])
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 md:gap-8">
        {Array.from({ length: limit || 8 }).map((_, i) => (
          <div key={i} className="animate-pulse">
            <div className="aspect-[3/4] bg-gray-200 rounded-md mb-4" />
            <div className="h-4 bg-gray-200 rounded w-3/4 mb-2" />
            <div className="h-4 bg-gray-200 rounded w-1/2" />
          </div>
        ))}
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          {error}
        </p>
      </div>
    )
  }

  const displayProducts = limit ? products.slice(0, limit) : products

  if (displayProducts.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          No products found.
        </p>
      </div>
    )
  }
  
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 md:gap-8">
      {displayProducts.map((product) => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  )
}
