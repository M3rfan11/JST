"use client"

import { useState, useEffect } from "react"
import { Header } from "@/components/header"
import { ProductDetail } from "@/components/product-detail"
import { notFound, useParams } from "next/navigation"
import { Button } from "@/components/ui/button"
import Link from "next/link"
import { ArrowLeft } from "lucide-react"
import { api } from "@/lib/api-client"

export default function ProductPage() {
  const params = useParams()
  const id = params.id as string
  const [product, setProduct] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  useEffect(() => {
    const loadProduct = async () => {
      try {
        setLoading(true)
        const productData = await api.products.getById(id) as any
        
        if (!productData) {
          setError(true)
          return
        }

        // Parse images from mediaUrls or use imageUrl
        let images: string[] = []
        if (productData.mediaUrls) {
          try {
            const parsed = typeof productData.mediaUrls === 'string' 
              ? JSON.parse(productData.mediaUrls) 
              : productData.mediaUrls
            if (Array.isArray(parsed)) {
              images = parsed
            }
          } catch {
            // If parsing fails, continue
          }
        }
        if (images.length === 0 && productData.imageUrl) {
          images = [productData.imageUrl]
        }
        if (images.length === 0) {
          images = ["/placeholder.svg"]
        }

        // Extract sizes from variants
        const sizes: string[] = []
        if (productData.variants && Array.isArray(productData.variants)) {
          productData.variants.forEach((variant: any) => {
            if (variant.attributes) {
              try {
                const attrs = typeof variant.attributes === 'string' 
                  ? JSON.parse(variant.attributes) 
                  : variant.attributes
                if (typeof attrs === 'object' && attrs !== null) {
                  // Look for Size attribute
                  const size = attrs['Size'] || attrs['size'] || attrs['SIZE']
                  if (size && !sizes.includes(size)) {
                    sizes.push(size)
                  }
                }
              } catch {
                // If parsing fails, skip
              }
            }
          })
        }

        // Get price - use first variant's priceOverride if available, otherwise use product price
        let price = productData.price || 0
        if (productData.variants && productData.variants.length > 0) {
          const variantWithPrice = productData.variants.find((v: any) => v.priceOverride && v.priceOverride > 0)
          if (variantWithPrice) {
            price = variantWithPrice.priceOverride
          }
        }

        setProduct({
          id: productData.id.toString(),
          name: productData.name,
          price: price,
          images: images,
          category: productData.categoryName || productData.category?.name || "Uncategorized",
          description: productData.description || "",
          details: productData.details || [],
          sizes: sizes.sort(),
          washingInstructions: productData.washingInstructions,
          variants: productData.variants || [],
        })
      } catch (err: any) {
        console.error("Error loading product:", err)
        setError(true)
      } finally {
        setLoading(false)
      }
    }

    if (id) {
      loadProduct()
    }
  }, [id])

  if (loading) {
    return (
      <div className="min-h-screen">
        <Header />
        <div className="container mx-auto px-4 sm:px-6 py-8">
          <div className="text-center" style={{ fontFamily: '"Dream Avenue"' }}>Loading product...</div>
        </div>
      </div>
    )
  }

  if (error || !product) {
    notFound()
  }

  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 py-4">
        <Button variant="ghost" asChild style={{ fontFamily: '"Dream Avenue"' }}>
          <Link href="/shop">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Shop
          </Link>
        </Button>
      </div>
      <ProductDetail product={product} />
    </div>
  )
}
