"use client"

import { usePathname } from "next/navigation"
import { Footer } from "./footer"

export function ConditionalFooter() {
  const pathname = usePathname()
  
  // Don't show footer on admin pages
  if (pathname?.startsWith("/admin")) {
    return null
  }
  
  // Remove top margin on collections page (section has dark background that should connect to footer)
  const noTopMargin = pathname === "/collections"
  
  return <Footer noTopMargin={noTopMargin} />
}



