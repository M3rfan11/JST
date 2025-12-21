"use client"

import { Header } from "@/components/header"
import { ProductGrid } from "@/components/product-grid"
import { Button } from "@/components/ui/button"
import Link from "next/link"
import { ArrowLeft } from "lucide-react"
import { useSearchParams } from "next/navigation"
import { Suspense } from "react"

function ShopContent() {
  const searchParams = useSearchParams()
  const categoryId = searchParams.get('category')
  const categoryIdNum = categoryId ? parseInt(categoryId, 10) : undefined

  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 md:px-6 py-8 md:py-12">
        <Button variant="ghost" asChild className="mb-4">
          <Link href="/">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Link>
        </Button>

        <h1 className="font-serif text-4xl md:text-5xl font-semibold mb-8" style={{ fontFamily: '"Dream Avenue"' }}>
          {categoryIdNum ? "COLLECTION" : "ALL PRODUCTS"}
        </h1>
        <ProductGrid categoryId={categoryIdNum} />
      </div>
    </div>
  )
}

export default function ShopPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen">
        <Header />
        <div className="container mx-auto px-4 md:px-6 py-8 md:py-12">
          <div className="text-center py-12">
            <p style={{ fontFamily: '"Dream Avenue"' }}>Loading...</p>
          </div>
        </div>
      </div>
    }>
      <ShopContent />
    </Suspense>
  )
}
