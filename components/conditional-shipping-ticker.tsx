"use client"

import { usePathname } from "next/navigation"
import { ShippingTicker } from "./shipping-ticker"

export function ConditionalShippingTicker() {
  const pathname = usePathname()
  
  // Don't show shipping ticker on admin pages
  if (pathname?.startsWith("/admin")) {
    return null
  }
  
  return <ShippingTicker />
}



