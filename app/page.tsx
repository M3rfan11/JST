"use client"

import { Header } from "@/components/header"
import { Button } from "@/components/ui/button"
import Link from "next/link"
import Image from "next/image"
import { ProductGrid } from "@/components/product-grid"
import { useState, useEffect, useRef } from "react"

export default function Home() {
  const [videoError, setVideoError] = useState(false)
  const [videoLoaded, setVideoLoaded] = useState(false)
  const [useFallback, setUseFallback] = useState(false)
  const videoRef = useRef<HTMLVideoElement>(null)

  useEffect(() => {
    // Check connection speed and set fallback if needed (only in browser)
    if (typeof window !== 'undefined' && 'connection' in navigator) {
      const connection = (navigator as any).connection || (navigator as any).mozConnection || (navigator as any).webkitConnection
      if (connection) {
        // If on slow connection (2G, 3G) or save-data mode, use fallback immediately
        if (
          connection.effectiveType === 'slow-2g' || 
          connection.effectiveType === '2g' || 
          connection.effectiveType === '3g' ||
          connection.saveData ||
          (connection.downlink && connection.downlink < 1.5) // Less than 1.5 Mbps
        ) {
          setUseFallback(true)
          return // Exit early, don't try to load video
        }
      }
    }

    // Set shorter timeout for video loading (2 seconds instead of 5)
    const loadTimeout = setTimeout(() => {
      if (!videoLoaded && videoRef.current) {
        const video = videoRef.current
        // If video hasn't loaded enough data, use fallback
        if (video.readyState < 2) { // HAVE_CURRENT_DATA
          setUseFallback(true)
        }
      }
    }, 2000) // Reduced from 5000ms to 2000ms

    return () => clearTimeout(loadTimeout)
  }, [videoLoaded])

  const handleVideoError = () => {
    setVideoError(true)
    setUseFallback(true)
  }

  const handleVideoLoaded = () => {
    setVideoLoaded(true)
  }

  return (
    <div className="min-h-screen">
      <Header />

      <section id="hero-video-section" className="relative h-[70vh] sm:h-[80vh] flex items-center justify-center overflow-hidden pt-16">
        <div className="absolute inset-0 bg-gradient-to-br from-slate-900 via-blue-900 to-slate-900">
          {!useFallback ? (
            <video
              ref={videoRef}
              autoPlay
              loop
              muted
              playsInline
              className="absolute inset-0 w-full h-full object-cover"
              preload="none"
              onError={handleVideoError}
              onLoadedData={handleVideoLoaded}
              onCanPlay={handleVideoLoaded}
              onLoadStart={() => {
                // If video takes too long to start loading, switch to fallback
                setTimeout(() => {
                  if (videoRef.current && videoRef.current.readyState === 0) {
                    setUseFallback(true)
                  }
                }, 1500)
              }}
            >
              <source src="/videoJST.mp4" type="video/mp4" />
              <source src="/videoJST.MOV" type="video/quicktime" />
            </video>
          ) : (
            <div className="absolute inset-0 w-full h-full">
              <Image
                src="/logo2.png"
                alt="JST Fashion"
                fill
                className="object-cover opacity-30"
                priority
              />
              <div className="absolute inset-0 bg-gradient-to-br from-slate-900 via-blue-900 to-slate-900" />
            </div>
          )}
          <div className="absolute inset-0 bg-black/40" />
        </div>

        <div className="relative z-10 text-center px-4 sm:px-6 max-w-4xl mx-auto">
          <h1 className="font-serif text-5xl sm:text-6xl md:text-7xl lg:text-8xl font-semibold mb-6 tracking-tight text-white" style={{ fontFamily: '"Dream Avenue"' }}>
            Refined Simplicity
          </h1>
          <p className="text-lg sm:text-xl md:text-2xl text-white/90 max-w-2xl mx-auto mb-8" style={{ fontFamily: '"Dream Avenue"' }}>
            Discover timeless pieces crafted for the modern wardrobe
          </p>
          <Button asChild size="lg" className="bg-foreground text-background hover:bg-foreground/90 h-12 px-8 text-base" style={{ backgroundColor: 'rgba(206, 180, 157, 1)', fontFamily: '"Dream Avenue"' }}>
            <Link href="/shop">Explore Collection</Link>
          </Button>
        </div>
      </section>

      {/* New Arrivals Section */}
      <section className="container mx-auto px-4 sm:px-6 py-12 sm:py-16 md:py-24">
        <div className="flex items-end justify-between mb-8 sm:mb-12">
          <h2 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>New Arrivals</h2>
          <Link href="/new" className="text-sm font-medium hover:text-muted-foreground transition-colors" style={{ fontFamily: '"Dream Avenue"' }}>
            View All →
          </Link>
        </div>
        <ProductGrid limit={4} categoryName="New Arrival" />
      </section>

      {/* Footer */}
      <footer className="border-t border-border mt-16 sm:mt-24" style={{ backgroundColor: 'rgba(206, 180, 157, 1)', borderTopColor: 'rgba(206, 180, 157, 1)' }}>
        <div className="container mx-auto px-4 sm:px-6 py-8 sm:py-12">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 sm:gap-8">
            <div>
              <div className="mb-4">
                <Image 
                  src="/logo2.png" 
                  alt="THUDARUM" 
                  width={50} 
                  height={50}
                  className="h-[50px] w-[50px]"
                />
              </div>
              <p className="text-sm text-muted-foreground" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>Contemporary fashion for the discerning individual.</p>
            </div>
            <div>
              <h4 className="font-medium mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Shop</h4>
              <ul className="space-y-2 text-sm text-muted-foreground" style={{ color: 'rgba(255, 255, 255, 1)' }}>
                <li>
                  <Link href="/shop" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>All Products</Link>
                </li>
                <li style={{ color: 'rgba(255, 255, 255, 1)' }}>
                  <Link href="/collections" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>Collections</Link>
                </li>
                <li>
                  <Link href="/new" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>New Arrivals</Link>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-4" style={{ color: 'rgba(0, 0, 0, 1)', fontFamily: '"Dream Avenue"' }}>Support</h4>
              <ul className="space-y-2 text-sm text-muted-foreground" style={{ color: 'rgba(255, 255, 255, 1)' }}>
                <li>
                  <Link href="/contact" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>Contact</Link>
                </li>
                <li>
                  <Link href="/shipping" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>Shipping</Link>
                </li>
                <li>
                  <Link href="/returns" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>Returns</Link>
                </li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Legal</h4>
              <ul className="space-y-2 text-sm text-muted-foreground" style={{ color: 'rgba(255, 255, 255, 1)' }}>
                <li>
                  <Link href="/privacy" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>Privacy</Link>
                </li>
                <li>
                  <Link href="/terms" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>Terms</Link>
                </li>
              </ul>
            </div>
          </div>
          <div className="border-t border-border mt-6 sm:mt-8 pt-6 sm:pt-8 text-center text-xs sm:text-sm text-muted-foreground">
            <p style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>&copy; 2025 JST. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  )
}
