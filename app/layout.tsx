import type React from "react"
import type { Metadata } from "next"
import { Analytics } from "@vercel/analytics/next"
import "./globals.css"
import { CartProvider } from "@/components/cart-provider"
import { AuthProvider } from "@/components/auth-provider"
import { SuperAdminGuard } from "@/components/superadmin-guard"
import { Toaster } from "@/components/ui/toaster"
import { ShippingTicker } from "@/components/shipping-ticker"
import { Footer } from "@/components/footer"

export const metadata: Metadata = {
  title: "Thudarum - Modern Fashion",
  description: "Minimalist contemporary fashion for the discerning individual",
  generator: "v0.app",
  icons: {
    icon: [
      {
        url: "/icon-light-32x32.png",
        media: "(prefers-color-scheme: light)",
      },
      {
        url: "/icon-dark-32x32.png",
        media: "(prefers-color-scheme: dark)",
      },
      {
        url: "/icon.svg",
        type: "image/svg+xml",
      },
    ],
    apple: "/apple-icon.png",
  },
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en">
      <body className="font-sans antialiased">
        <AuthProvider>
          <SuperAdminGuard>
            <CartProvider>
              <ShippingTicker />
              {children}
              <Footer />
              <Toaster />
            </CartProvider>
          </SuperAdminGuard>
        </AuthProvider>
        <Analytics />
      </body>
    </html>
  )
}
