"use client"

import { useState, useEffect } from "react"
import { Header } from "@/components/header"
import { ProductGrid } from "@/components/product-grid"
import { Button } from "@/components/ui/button"
import { Select } from "@/components/ui/select"
import { Label } from "@/components/ui/label"
import Link from "next/link"
import { ArrowLeft, Filter } from "lucide-react"
import { useSearchParams } from "next/navigation"
import { Suspense } from "react"
import { api } from "@/lib/api-client"

interface Category {
  id: number
  name: string
}

type SortOption = "default" | "price-low" | "price-high" | "name-asc" | "name-desc"

function ShopContent() {
  const searchParams = useSearchParams()
  const categoryId = searchParams.get('category')
  const categoryIdNum = categoryId ? parseInt(categoryId, 10) : undefined

  const [categories, setCategories] = useState<Category[]>([])
  const [selectedCategory, setSelectedCategory] = useState<string>(categoryIdNum ? String(categoryIdNum) : "all")
  const [sortBy, setSortBy] = useState<SortOption>("default")
  const [loadingCategories, setLoadingCategories] = useState(true)

  useEffect(() => {
    loadCategories()
  }, [])

  // Update selected category when URL param changes
  useEffect(() => {
    if (categoryIdNum) {
      setSelectedCategory(String(categoryIdNum))
    } else {
      setSelectedCategory("all")
    }
  }, [categoryIdNum])

  // Get the selected category name for filtering
  const selectedCategoryName = selectedCategory !== "all" 
    ? categories.find(cat => cat.id === parseInt(selectedCategory))?.name 
    : undefined

  const loadCategories = async () => {
    try {
      setLoadingCategories(true)
      const cats = await api.categories.getPublic() as Category[]
      setCategories(cats)
    } catch (error) {
      console.error("Error loading categories:", error)
      setCategories([])
    } finally {
      setLoadingCategories(false)
    }
  }

  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 md:px-6 py-8 md:py-12 pt-28">
        <Button variant="ghost" asChild className="mb-6" style={{ fontFamily: '"Dream Avenue"' }}>
          <Link href="/">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Link>
        </Button>

        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
          <h1 className="font-serif text-4xl md:text-5xl font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>
            {categoryIdNum ? "COLLECTION" : "ALL PRODUCTS"}
          </h1>
          
          {/* Filters */}
          <div className="flex flex-wrap items-center gap-4">
            <div className="flex items-center gap-2">
              <Filter className="h-4 w-4 text-muted-foreground" />
              <Label htmlFor="category-filter" className="text-sm whitespace-nowrap" style={{ fontFamily: '"Dream Avenue"' }}>
                Category:
              </Label>
              <Select
                id="category-filter"
                value={selectedCategory}
                onChange={(e) => setSelectedCategory(e.target.value)}
                className="min-w-[150px]"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                <option value="all">All Categories</option>
                {categories.map((cat) => (
                  <option key={cat.id} value={cat.id}>
                    {cat.name}
                  </option>
                ))}
              </Select>
            </div>

            <div className="flex items-center gap-2">
              <Label htmlFor="sort-filter" className="text-sm whitespace-nowrap" style={{ fontFamily: '"Dream Avenue"' }}>
                Sort by:
              </Label>
              <Select
                id="sort-filter"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as SortOption)}
                className="min-w-[180px]"
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                <option value="default">Default</option>
                <option value="price-low">Price: Low to High</option>
                <option value="price-high">Price: High to Low</option>
                <option value="name-asc">Name: A to Z</option>
                <option value="name-desc">Name: Z to A</option>
              </Select>
            </div>
          </div>
        </div>

        <ProductGrid 
          categoryId={selectedCategory !== "all" ? parseInt(selectedCategory) : undefined}
          categoryName={selectedCategoryName}
          sortBy={sortBy}
        />
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
