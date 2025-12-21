"use client"

import Link from "next/link"
import Image from "next/image"
import { ShoppingBag, User, Menu, LogOut } from "lucide-react"
import { useCart } from "./cart-provider"
import { useAuth } from "./auth-provider"
import { Button } from "./ui/button"
import { Sheet, SheetContent, SheetTitle, SheetTrigger } from "./ui/sheet"
import { useState, useEffect } from "react"
import { usePathname } from "next/navigation"

export function Header() {
  const { itemCount } = useCart()
  const { isAuthenticated, logout, user, isSuperAdmin } = useAuth()
  const [open, setOpen] = useState(false)
  const [isOverVideo, setIsOverVideo] = useState(true)
  const [mounted, setMounted] = useState(false)
  const pathname = usePathname()
  const isHomePage = pathname === "/"

  useEffect(() => {
    setMounted(true)
    // Only apply video scroll logic on home page
    if (!isHomePage) {
      setIsOverVideo(false)
      return
    }

    const handleScroll = () => {
      const videoSection = document.getElementById('hero-video-section')
      if (videoSection) {
        const videoBottom = videoSection.offsetTop + videoSection.offsetHeight
        const scrollPosition = window.scrollY + 28 + 80 // header top + header height
        setIsOverVideo(scrollPosition < videoBottom)
      }
    }

    window.addEventListener('scroll', handleScroll)
    handleScroll() // Check initial state

    return () => window.removeEventListener('scroll', handleScroll)
  }, [isHomePage])

  // On non-home pages, always use white header
  const shouldBeTransparent = isHomePage && isOverVideo

  return (
    <header 
      className={`fixed top-[28px] left-0 right-0 w-full z-40 transition-all duration-300 ${
        shouldBeTransparent 
          ? 'bg-transparent' 
          : 'bg-white border-b border-border'
      }`}
      style={{ backgroundColor: shouldBeTransparent ? 'transparent' : 'white' }}
    >
      <div className="container mx-auto flex h-20 items-center justify-between px-4 md:px-6">
        {/* Logo */}
        <Link href="/" className="flex flex-wrap font-serif text-2xl font-semibold tracking-tight">
          <Image 
            src={shouldBeTransparent ? "/logo-white.png" : "/logo2.png"} 
            alt="Logo" 
            width={100} 
            height={100} 
          />
        </Link>

        {/* Desktop Navigation */}
        <nav className="hidden md:flex items-center gap-8">
          <Link 
            href="/shop" 
            className={`text-base font-medium transition-colors ${
              shouldBeTransparent 
                ? 'text-white hover:text-white/80' 
                : 'text-foreground hover:text-muted-foreground'
            }`}
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            Shop
          </Link>
          <Link 
            href="/collections" 
            className={`text-base font-medium transition-colors ${
              shouldBeTransparent 
                ? 'text-white hover:text-white/80' 
                : 'text-foreground hover:text-muted-foreground'
            }`}
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            Collections
          </Link>
          <Link 
            href="/about" 
            className={`text-base font-medium transition-colors ${
              shouldBeTransparent 
                ? 'text-white hover:text-white/80' 
                : 'text-foreground hover:text-muted-foreground'
            }`}
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            About
          </Link>
          <Link 
            href="/track" 
            className={`text-base font-medium transition-colors ${
              shouldBeTransparent 
                ? 'text-white hover:text-white/80' 
                : 'text-foreground hover:text-muted-foreground'
            }`}
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            Track Order
          </Link>
        </nav>

        {/* Actions */}
        <div className="flex items-center gap-4">
          {isAuthenticated ? (
            <>
              {isSuperAdmin ? (
                <Link href="/admin">
                  <Button 
                    variant="ghost" 
                    size="sm"
                    className={`hidden md:flex transition-colors ${
                      shouldBeTransparent 
                        ? 'text-white hover:text-white/80' 
                        : 'text-foreground hover:text-muted-foreground'
                    }`}
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    Admin Dashboard
                  </Button>
                </Link>
              ) : (
                <Link href="/profile">
                  <Button 
                    variant="ghost" 
                    size="icon" 
                    className={`hidden md:flex transition-colors ${
                      shouldBeTransparent 
                        ? 'text-white hover:text-white/80' 
                        : 'text-foreground hover:text-muted-foreground'
                    }`}
                  >
                    <User className="h-6 w-6" />
                    <span className="sr-only">Profile</span>
                  </Button>
                </Link>
              )}
              <Button 
                variant="ghost" 
                size="icon" 
                onClick={logout}
                className={`hidden md:flex transition-colors ${
                  shouldBeTransparent 
                    ? 'text-white hover:text-white/80' 
                    : 'text-foreground hover:text-muted-foreground'
                }`}
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                <LogOut className="h-6 w-6" />
                <span className="sr-only">Logout</span>
              </Button>
            </>
          ) : (
            <Link href="/login">
              <Button 
                variant="ghost" 
                size="sm"
                className={`hidden md:flex transition-colors ${
                  shouldBeTransparent 
                    ? 'text-white hover:text-white/80' 
                    : 'text-foreground hover:text-muted-foreground'
                }`}
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                Login
              </Button>
            </Link>
          )}
          <Link href="/cart">
            <Button 
              variant="ghost" 
              size="icon" 
              className={`relative transition-colors ${
                shouldBeTransparent 
                  ? 'text-white hover:text-white/80' 
                  : 'text-foreground hover:text-muted-foreground'
              }`}
            >
              <ShoppingBag className="h-6 w-6" />
              {itemCount > 0 && (
                <span className="absolute -top-1 -right-1 h-5 w-5 rounded-full bg-foreground text-background text-xs flex items-center justify-center font-medium animate-in zoom-in-50 duration-200" style={{ backgroundColor: 'rgba(61, 8, 17, 1)' }}>
                  {itemCount}
                </span>
              )}
              <span className="sr-only">Cart ({itemCount} items)</span>
            </Button>
          </Link>

          {mounted ? (
            <Sheet open={open} onOpenChange={setOpen}>
              <SheetTrigger asChild>
                <Button 
                  variant="ghost" 
                  size="icon" 
                  className={`md:hidden transition-colors ${
                    shouldBeTransparent 
                      ? 'text-white hover:text-white/80' 
                      : 'text-foreground hover:text-muted-foreground'
                  }`}
                >
                  <Menu className="h-6 w-6" />
                  <span className="sr-only">Menu</span>
                </Button>
              </SheetTrigger>
              <SheetContent
                side="right"
                className="w-[85vw] sm:w-[350px] px-6 [&>button]:border-0 [&>button]:shadow-none [&>button]:ring-0 [&>button]:top-8"
              >
                <SheetTitle className="sr-only">Navigation Menu</SheetTitle>
                <div className="flex flex-col gap-8 pt-8">
                  <div className="mb-4">
                    <Image 
                      src="/logo2.png" 
                      alt="JST" 
                      width={50} 
                      height={50}
                      className="h-[50px] w-[50px]"
                    />
                  </div>

                  <nav className="flex flex-col gap-0">
                    <Link
                      href="/shop"
                      onClick={() => setOpen(false)}
                      className="text-lg font-medium py-5 px-4 border-b border-border hover:bg-accent transition-colors"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      Shop
                    </Link>
                    <Link
                      href="/collections"
                      onClick={() => setOpen(false)}
                      className="text-lg font-medium py-5 px-4 border-b border-border hover:bg-accent transition-colors"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      Collections
                    </Link>
                    <Link
                      href="/about"
                      onClick={() => setOpen(false)}
                      className="text-lg font-medium py-5 px-4 border-b border-border hover:bg-accent transition-colors"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      About
                    </Link>
                    <Link
                      href="/track"
                      onClick={() => setOpen(false)}
                      className="text-lg font-medium py-5 px-4 border-b border-border hover:bg-accent transition-colors"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      Track Order
                    </Link>
                    {isAuthenticated ? (
                      <>
                        {isSuperAdmin ? (
                          <Link
                            href="/admin"
                            onClick={() => setOpen(false)}
                            className="text-lg font-medium py-5 px-4 border-b border-border hover:bg-accent transition-colors"
                            style={{ fontFamily: '"Dream Avenue"' }}
                          >
                            Admin Dashboard
                          </Link>
                        ) : (
                          <Link
                            href="/profile"
                            onClick={() => setOpen(false)}
                            className="text-lg font-medium py-5 px-4 border-b border-border hover:bg-accent transition-colors"
                            style={{ fontFamily: '"Dream Avenue"' }}
                          >
                            Profile
                          </Link>
                        )}
                        <button
                          onClick={() => {
                            logout()
                            setOpen(false)
                          }}
                          className="text-lg font-medium py-5 px-4 border-b border-border hover:bg-accent transition-colors text-left"
                          style={{ fontFamily: '"Dream Avenue"' }}
                        >
                          Logout
                        </button>
                      </>
                    ) : (
                      <Link
                        href="/login"
                        onClick={() => setOpen(false)}
                        className="text-lg font-medium py-5 px-4 border-b border-border hover:bg-accent transition-colors"
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        Login
                      </Link>
                    )}
                    <Link
                      href="/cart"
                      onClick={() => setOpen(false)}
                      className="text-lg font-medium py-5 px-4 border-b border-border hover:bg-accent transition-colors flex items-center justify-between"
                      style={{ fontFamily: '"Dream Avenue"' }}
                    >
                      <span>Cart</span>
                      {itemCount > 0 && (
                        <span className="h-6 w-6 rounded-full bg-foreground text-background text-xs flex items-center justify-center font-medium" style={{ backgroundColor: 'rgba(61, 8, 17, 1)', fontFamily: '"Dream Avenue"' }}>
                          {itemCount}
                        </span>
                      )}
                    </Link>
                  </nav>
                </div>
              </SheetContent>
            </Sheet>
          ) : (
            <Button 
              variant="ghost" 
              size="icon" 
              className={`md:hidden transition-colors ${
                shouldBeTransparent 
                  ? 'text-white hover:text-white/80' 
                  : 'text-foreground hover:text-muted-foreground'
              }`}
              disabled
            >
              <Menu className="h-5 w-5" />
              <span className="sr-only">Menu</span>
            </Button>
          )}
        </div>
      </div>
    </header>
  )
}
