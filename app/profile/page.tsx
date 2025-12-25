"use client"

import { Header } from "@/components/header"

import { ProfileSettings } from "@/components/profile-settings"

import { useSearchParams, useRouter } from "next/navigation"
import { Suspense, useEffect } from "react"
import { Button } from "@/components/ui/button"
import Link from "next/link"
import { ArrowLeft } from "lucide-react"
import { useAuth } from "@/components/auth-provider"

function ProfileContent() {
  const searchParams = useSearchParams()
  const router = useRouter()
  const { isAuthenticated } = useAuth()
  const tabParam = searchParams.get("tab")
  // Default to profile, or if orders tab is requested, redirect to profile
  const defaultTab = tabParam === "orders" ? "profile" : (tabParam || "profile")

  useEffect(() => {
    if (!isAuthenticated) {
      router.push("/login")
    }
  }, [isAuthenticated, router])

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p style={{ fontFamily: '"Dream Avenue"' }}>Redirecting to login...</p>
      </div>
    )
  }

  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 py-4">
        <Button variant="ghost" asChild style={{ fontFamily: '"Dream Avenue"' }}>
          <Link href="/">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Link>
        </Button>
      </div>
      <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8 md:py-12 max-w-7xl">
        <h1 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-8 sm:mb-10" style={{ fontFamily: '"Dream Avenue"' }}>My Account</h1>

        <div className="space-y-8 sm:space-y-10">
          <ProfileSettings />
        </div>
      </div>
    </div>
  )
}

export default function ProfilePage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <ProfileContent />
    </Suspense>
  )
}
