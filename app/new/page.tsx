"use client"

import { Header } from "@/components/header"
import { ProductGrid } from "@/components/product-grid"
import { Button } from "@/components/ui/button"
import Link from "next/link"
import { ArrowLeft } from "lucide-react"

export default function NewArrivalsPage() {
  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 md:px-6 py-8 md:py-12 pt-40 mt-12">
        <Button variant="ghost" asChild className="mb-6" style={{ fontFamily: '"Dream Avenue"' }}>
          <Link href="/">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Link>
        </Button>

        <h1 className="font-serif text-4xl md:text-5xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
          New Arrivals
        </h1>
        <ProductGrid categoryName="New Arrival" />
      </div>
    </div>
  )
}

