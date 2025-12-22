"use client"

import { Header } from "@/components/header"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ProfileSettings } from "@/components/profile-settings"
import { SavedAddresses } from "@/components/saved-addresses"
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

        <Tabs defaultValue={defaultTab} className="space-y-8 sm:space-y-10">
          <TabsList className="grid w-full grid-cols-2 h-12 sm:h-14" style={{ backgroundColor: '#3D0811' }}>
            <TabsTrigger 
              value="profile" 
              className="text-base sm:text-lg font-medium data-[state=active]:bg-white data-[state=active]:text-[#3D0811] text-white" 
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              Profile
            </TabsTrigger>
            <TabsTrigger 
              value="addresses" 
              className="text-base sm:text-lg font-medium data-[state=active]:bg-white data-[state=active]:text-[#3D0811] text-white" 
              style={{ fontFamily: '"Dream Avenue"' }}
            >
              Addresses
            </TabsTrigger>
          </TabsList>

          <TabsContent value="profile">
            <ProfileSettings />
          </TabsContent>

          <TabsContent value="addresses">
            <SavedAddresses />
          </TabsContent>
        </Tabs>
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
