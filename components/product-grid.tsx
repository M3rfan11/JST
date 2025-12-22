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

type SortOption = "default" | "price-low" | "price-high" | "name-asc" | "name-desc"

export function ProductGrid({ 
  limit, 
  categoryId, 
  categoryName,
  sortBy = "default"
}: { 
  limit?: number
  categoryId?: number
  categoryName?: string
  sortBy?: SortOption
}) {
  const [allProducts, setAllProducts] = useState<Product[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const applySorting = (productsToSort: Product[], sortOption: SortOption) => {
    let sorted = [...productsToSort]
    switch (sortOption) {
      case "price-low":
        sorted.sort((a, b) => a.price - b.price)
        break
      case "price-high":
        sorted.sort((a, b) => b.price - a.price)
        break
      case "name-asc":
        sorted.sort((a, b) => a.name.localeCompare(b.name))
        break
      case "name-desc":
        sorted.sort((a, b) => b.name.localeCompare(a.name))
        break
      default:
        // Keep original order
        break
    }
    setProducts(sorted)
  }

  // Load products when category changes
  useEffect(() => {
    loadProducts()
  }, [categoryId, categoryName])

  // Apply sorting when sortBy changes
  useEffect(() => {
    if (allProducts.length > 0) {
      applySorting(allProducts, sortBy)
    }
  }, [sortBy, allProducts])

  const loadProducts = async () => {
    try {
      setLoading(true)
      setError(null)
      
      // Fetch products from API (using public endpoint that doesn't require auth)
      // Pass categoryName to filter on the backend if provided
      const response = await api.products.getPublic(categoryName ? { categoryName } : undefined) as any
      
      // Handle different response formats
      let productsList: any[] = []
      if (Array.isArray(response)) {
        productsList = response
      } else if (response && Array.isArray(response.value)) {
        productsList = response.value
      } else if (response && response.data && Array.isArray(response.data)) {
        productsList = response.data
      }
      
      // Note: Category filtering is handled by the backend API when categoryName is passed
      // The API doesn't return categoryId/categoryIds in the response, so we rely on backend filtering
      // If categoryId is provided but categoryName is not, we can't filter on frontend
      // This should be handled by passing categoryName from the selected category
      
      // Map API response to ProductCard format
      const mappedProducts: Product[] = productsList
        .filter((p: any) => p.isActive !== false && (p.status === 1 || p.status === 'Active' || p.status === undefined))
        .map((p: any) => {
          // Get first image from mediaUrls, imageUrl, or variants
          let imageUrl = "/placeholder.svg"
          
          // Try product-level mediaUrls first
          if (p.mediaUrls) {
            try {
              const mediaUrls = typeof p.mediaUrls === 'string' ? JSON.parse(p.mediaUrls) : p.mediaUrls
              if (Array.isArray(mediaUrls) && mediaUrls.length > 0 && mediaUrls[0]) {
                imageUrl = mediaUrls[0]
              }
            } catch {
              // If parsing fails, continue to next option
            }
          }
          
          // Fallback to product imageUrl if mediaUrls didn't work
          if (imageUrl === "/placeholder.svg" && p.imageUrl) {
            imageUrl = p.imageUrl
          }
          
          // Fallback to variant images if product has no image
          if (imageUrl === "/placeholder.svg" && p.variants && Array.isArray(p.variants) && p.variants.length > 0) {
            for (const variant of p.variants) {
              if (variant.mediaUrls) {
                try {
                  const variantMediaUrls = typeof variant.mediaUrls === 'string' ? JSON.parse(variant.mediaUrls) : variant.mediaUrls
                  if (Array.isArray(variantMediaUrls) && variantMediaUrls.length > 0 && variantMediaUrls[0]) {
                    imageUrl = variantMediaUrls[0]
                    break
                  }
                } catch {
                  // Continue to next variant
                }
              }
              if (imageUrl === "/placeholder.svg" && variant.imageUrl) {
                imageUrl = variant.imageUrl
                break
              }
            }
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
      
      setAllProducts(mappedProducts)
      applySorting(mappedProducts, sortBy)
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
