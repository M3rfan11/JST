"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { Header } from "@/components/header"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuth } from "@/components/auth-provider"
import { useToast } from "@/hooks/use-toast"
import { ArrowLeft } from "lucide-react"

export default function LoginPage() {
  const router = useRouter()
  const { login } = useAuth()
  const { toast } = useToast()
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)

    const result = await login(email, password)

    if (result.success) {
      toast({
        title: "Welcome back!",
        description: "You have been successfully logged in.",
      })
      // Redirect based on user role (SuperAdmin -> /admin, others -> /profile)
      router.push(result.redirectTo || "/profile")
    } else {
      toast({
        title: "Login failed",
        description: "Invalid email or password. Please try again.",
        variant: "destructive",
      })
    }

    setIsLoading(false)
  }

  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 py-8 md:py-12 max-w-md pt-40 mt-12">
        <div className="mt-8">
          <Button 
            variant="ghost" 
            asChild 
            className="mb-6 transition-opacity duration-300" 
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            <Link href="/">
              <ArrowLeft className="h-4 w-4 mr-2 " />
              Back to Home
            </Link>
          </Button>
        </div>

        <div className="space-y-6">
          <div>
            <h1 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
              Login
            </h1>
            <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
              Sign in to your account to track orders and use coupons
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email" className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                Email
              </Label>
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                className="h-12"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="password" className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                Password
              </Label>
              <Input
                id="password"
                type="password"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="h-12"
              />
            </div>

            <Button
              type="submit"
              size="lg"
              className="w-full h-12"
              disabled={isLoading}
              style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
            >
              {isLoading ? "Signing in..." : "Sign In"}
            </Button>
          </form>

          <div className="text-center text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
            <span className="text-muted-foreground">Don't have an account? </span>
            <Link href="/signup" className="text-foreground hover:underline font-medium">
              Sign up
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}

