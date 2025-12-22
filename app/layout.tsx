import type React from "react"
import type { Metadata } from "next"
import { Analytics } from "@vercel/analytics/next"
import "./globals.css"
import { CartProvider } from "@/components/cart-provider"
import { AuthProvider } from "@/components/auth-provider"
import { SuperAdminGuard } from "@/components/superadmin-guard"
import { Toaster } from "@/components/ui/toaster"
import { ConditionalShippingTicker } from "@/components/conditional-shipping-ticker"
import { ConditionalFooter } from "@/components/conditional-footer"

export const metadata: Metadata = {
  title: "JST - Modern Fashion",
  description: "Minimalist contemporary fashion for the discerning individual",
  generator: "v0.app",
  icons: {
    icon: "/logo2.png",
    apple: "/logo2.png",
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
              <ConditionalShippingTicker />
              {children}
              <ConditionalFooter />
              <Toaster />
            </CartProvider>
          </SuperAdminGuard>
        </AuthProvider>
        <Analytics />
      </body>
    </html>
  )
}
