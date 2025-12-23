"use client"

import Link from "next/link"
import Image from "next/image"

export function Footer() {
  return (
    <footer className="border-t border-border mt-16 sm:mt-24" style={{ backgroundColor: 'rgba(206, 180, 157, 1)', borderTopColor: 'rgba(206, 180, 157, 1)' }}>
      <div className="container mx-auto px-4 sm:px-6 py-8 sm:py-12">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-6 sm:gap-8">
          <div>
            <div className="mb-4">
              <Image 
                src="/logo2.png" 
                alt="JST" 
                width={50} 
                height={50}
                className="h-[50px] w-[50px]"
              />
            </div>
            <p className="text-sm text-muted-foreground" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
              Contemporary fashion for the discerning individual.
            </p>
          </div>
          <div>
            <h4 className="font-medium mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Shop</h4>
            <ul className="space-y-2 text-sm text-muted-foreground" style={{ color: 'rgba(255, 255, 255, 1)' }}>
              <li>
                <Link href="/shop" className="hover:underline" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                  All Products
                </Link>
              </li>
              <li>
                <Link href="/collections" className="hover:underline" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                  Collections
                </Link>
              </li>
              <li>
                <Link href="/new" className="hover:underline" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                  New Arrivals
                </Link>
              </li>
            </ul>
          </div>
          <div>
            <h4 className="font-medium mb-4" style={{ color: 'rgba(0, 0, 0, 1)', fontFamily: '"Dream Avenue"' }}>Support</h4>
            <ul className="space-y-2 text-sm text-muted-foreground" style={{ color: 'rgba(255, 255, 255, 1)' }}>
              <li>
                <Link href="/about" className="hover:underline" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                  About
                </Link>
              </li>
              <li>
                <Link href="/track" className="hover:underline" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                  Track Order
                </Link>
              </li>
              <li>
                <Link href="/contact" className="hover:underline" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                  Contact
                </Link>
              </li>
            </ul>
          </div>
          <div>
            <h4 className="font-medium mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Legal</h4>
            <ul className="space-y-2 text-sm text-muted-foreground" style={{ color: 'rgba(255, 255, 255, 1)' }}>
              <li>
                <Link href="/privacy" className="hover:underline" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                  Privacy
                </Link>
              </li>
              <li>
                <Link href="/terms" className="hover:underline" style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                  Terms
                </Link>
              </li>
            </ul>
          </div>
        </div>
        <div className="border-t border-border mt-6 sm:mt-8 pt-6 sm:pt-8 text-center text-xs sm:text-sm text-muted-foreground">
          <p style={{ color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
            &copy; 2025 JST. All rights reserved.
          </p>
        </div>
      </div>
    </footer>
  )
}




