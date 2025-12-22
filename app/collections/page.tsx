"use client"

import { Header } from "@/components/header"
import Image from "next/image"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { ArrowLeft } from "lucide-react"
import { api } from "@/lib/api-client"
import { useEffect, useState, useRef } from "react"

interface Category {
  id: number
  name: string
  description: string | null
  imageUrls: string | null // JSON array of image URLs
  isActive: boolean
  productCount: number
}

export default function CollectionsPage() {
  const [collections, setCollections] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [currentImageIndex, setCurrentImageIndex] = useState<{ [key: number]: number }>({})
  const intervalRefs = useRef<{ [key: number]: NodeJS.Timeout }>({})

  useEffect(() => {
    loadCollections()
  }, [])

  const loadCollections = async () => {
    try {
      setLoading(true)
      const categories = await api.categories.getPublic() as Category[]
      
      // Filter for "JST for Her" and "JST for Him" collections
      const filteredCategories = categories.filter(
        (cat) => 
          cat.name === "JST for Her" || 
          cat.name === "JST for Him" ||
          cat.name.toLowerCase().includes("for her") ||
          cat.name.toLowerCase().includes("for him")
      )
      
      setCollections(filteredCategories)
    } catch (error) {
      console.error("Error loading collections:", error)
      setCollections([])
    } finally {
      setLoading(false)
    }
  }

  const parseImageUrls = (imageUrls: string | null): string[] => {
    if (!imageUrls) return []
    try {
      const parsed = JSON.parse(imageUrls)
      return Array.isArray(parsed) ? parsed : []
    } catch {
      return []
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen">
        <Header />
        <div className="container mx-auto px-4 sm:px-6 py-12 sm:py-16 md:py-24">
          <div className="flex items-center justify-center h-64">
            <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading collections...</div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen">
      <Header />

      {/* Hero Section */}
      <section className="container mx-auto px-4 sm:px-6 py-12 sm:py-16 md:py-24 pt-40 mt-12">
        <Button variant="ghost" asChild className="mb-6" style={{ fontFamily: '"Dream Avenue"' }}>
          <Link href="/" style={{ fontFamily: '"Dream Avenue"' }}>
            <ArrowLeft className="h-4 w-4 mr-2" />
            <span style={{ fontFamily: '"Dream Avenue"' }}>Back to Home</span>
          </Link>
        </Button>

        <div className="max-w-3xl mx-auto text-center mb-12 sm:mb-16 md:mb-24">
          <h1 className="font-serif text-4xl sm:text-5xl md:text-6xl lg:text-7xl font-semibold mb-6 tracking-tight" style={{ fontFamily: '"Dream Avenue"' }}>
            Collections
          </h1>
          <p className="text-base sm:text-lg md:text-xl text-muted-foreground text-balance" style={{ fontFamily: '"Dream Avenue"' }}>
            Explore our curated collections, each telling a unique story of style, craftsmanship, and modern elegance.
          </p>
        </div>

        {/* Collections Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 sm:gap-8 lg:gap-12">
          {collections.length === 0 ? (
            <div className="col-span-2 text-center py-12">
              <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                No collections found. Please create "JST for Her" and "JST for Him" categories in the dashboard.
              </p>
            </div>
          ) : (
            collections.map((collection) => {
              const images = parseImageUrls(collection.imageUrls)
              const primaryImage = images[0] || "/placeholder.svg"
              const hasMultipleImages = images.length > 1
              const currentIndex = currentImageIndex[collection.id] ?? 0
              const currentImage = images[currentIndex] || primaryImage
              
              return (
                <Link 
                  key={collection.id} 
                  href={`/shop?category=${collection.id}`} 
                  className="group"
                  onMouseEnter={() => {
                    if (hasMultipleImages && images.length > 1) {
                      // Start cycling through images with smooth transitions
                      let index = 0
                      const interval = setInterval(() => {
                        index = (index + 1) % images.length
                        setCurrentImageIndex(prev => ({ ...prev, [collection.id]: index }))
                      }, 2500) // Change image every 2.5 seconds
                      // Store interval ID to clear it later
                      intervalRefs.current[collection.id] = interval
                    }
                  }}
                  onMouseLeave={() => {
                    if (hasMultipleImages) {
                      // Clear interval and reset to first image
                      const interval = intervalRefs.current[collection.id]
                      if (interval) {
                        clearInterval(interval)
                        delete intervalRefs.current[collection.id]
                      }
                      setCurrentImageIndex(prev => ({ ...prev, [collection.id]: 0 }))
                    }
                  }}
                >
                  <div className="relative aspect-[4/5] overflow-hidden bg-secondary mb-4">
                    {/* Display all images layered, show current one */}
                    {images.map((img, idx) => (
                      <Image
                        key={`${collection.id}-${idx}`}
                        src={img || "/placeholder.svg"}
                        alt={collection.name}
                        fill
                        className={`object-cover transition-opacity duration-700 ease-in-out ${
                          idx === currentIndex ? 'opacity-100' : 'opacity-0'
                        }`}
                        style={{ position: 'absolute' }}
                        priority={idx === 0}
                      />
                    ))}
                    <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500" />
                    <div className="absolute bottom-0 left-0 right-0 p-6 sm:p-8 translate-y-4 group-hover:translate-y-0 transition-transform duration-500">
                      <p className="text-white/80 text-xs sm:text-sm mb-2" style={{ fontFamily: '"Dream Avenue"' }}>
                        {collection.productCount} {collection.productCount === 1 ? 'item' : 'items'}
                      </p>
                      <h2 className="font-serif text-2xl sm:text-3xl md:text-4xl font-semibold text-white mb-2" style={{ fontFamily: '"Dream Avenue"' }}>
                        {collection.name}
                      </h2>
                      <p className="text-white/90 text-sm sm:text-base opacity-0 group-hover:opacity-100 transition-opacity duration-500 delay-100" style={{ fontFamily: '"Dream Avenue"' }}>
                        {collection.description || "Discover our curated selection"}
                      </p>
                    </div>
                  </div>
                  <Button variant="ghost" className="w-full justify-between text-sm sm:text-base group-hover:bg-secondary" style={{ fontFamily: '"Dream Avenue"' }}>
                    View Collection
                    <span className="group-hover:translate-x-2 transition-transform duration-300">→</span>
                  </Button>
                </Link>
              )
            })
          )}
        </div>
      </section>

      {/* Featured Section */}
      <section className="py-12 sm:py-16 md:py-24" style={{ backgroundColor: '#3D0811' }}>
        <div className="container mx-auto px-4 sm:px-6">
          <div className="max-w-3xl mx-auto text-center">
            <h2 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-6 text-white" style={{ fontFamily: '"Dream Avenue"' }}>Crafted for Excellence</h2>
            <p className="mb-8 text-base sm:text-lg leading-relaxed text-white" style={{ fontFamily: '"Dream Avenue"' }}>
              Each collection is carefully curated to offer a distinct aesthetic while maintaining the exceptional
              quality and attention to detail that defines JST. From boardroom to ballroom, we have the perfect
              piece for every occasion.
            </p>
            <Button asChild size="lg" variant="outline" className="h-12 px-8 bg-transparent border-white text-white hover:bg-white/10" style={{ fontFamily: '"Dream Avenue"' }}>
              <Link href="/shop" className="text-white">Browse All Products</Link>
            </Button>
          </div>
        </div>
      </section>
    </div>
  )
}
