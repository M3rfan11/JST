"use client"

import type React from "react"

import Link from "next/link"
import Image from "next/image"

interface Product {
  id: string
  name: string
  price: number
  image: string
  category: string
}

export function ProductCard({ product }: { product: Product }) {

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
