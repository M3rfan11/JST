"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { Header } from "@/components/header"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuth } from "@/components/auth-provider"
import { useToast } from "@/hooks/use-toast"
import { ArrowLeft } from "lucide-react"

export default function SignupPage() {
  const router = useRouter()
  const { signup } = useAuth()
  const { toast } = useToast()
  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    email: "",
    password: "",
    confirmPassword: "",
  })
  const [isLoading, setIsLoading] = useState(false)
  const [showBackButton, setShowBackButton] = useState(false)

  useEffect(() => {
    const handleScroll = () => {
      // Show back button when scrolled down more than 100px
      setShowBackButton(window.scrollY > 100)
    }

    window.addEventListener('scroll', handleScroll)
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value })
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (formData.password !== formData.confirmPassword) {
      toast({
        title: "Password mismatch",
        description: "Passwords do not match. Please try again.",
        variant: "destructive",
      })
      return
    }

    if (formData.password.length < 6) {
      toast({
        title: "Password too short",
        description: "Password must be at least 6 characters long.",
        variant: "destructive",
      })
      return
    }

    setIsLoading(true)

    const success = await signup(
      formData.email,
      formData.password,
      formData.firstName,
      formData.lastName
    )

    if (success) {
      toast({
        title: "Account created!",
        description: "Welcome! Your account has been created successfully.",
      })
      router.push("/profile")
    } else {
      toast({
        title: "Signup failed",
        description: "An account with this email already exists. Please login instead.",
        variant: "destructive",
      })
    }

    setIsLoading(false)
  }

  return (
    <div className="min-h-screen">
      <Header />
      <div className="container mx-auto px-4 sm:px-6 py-8 md:py-12 max-w-md pt-28">
        {showBackButton && (
          <Button 
            variant="ghost" 
            asChild 
            className="mb-6 transition-opacity duration-300" 
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            <Link href="/">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Home
            </Link>
          </Button>
        )}

        <div className="space-y-6">
          <div>
            <h1 className="font-serif text-3xl sm:text-4xl md:text-5xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"' }}>
              Sign Up
            </h1>
            <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
              Create an account to track orders and use exclusive coupons
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName" className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                  First Name
                </Label>
                <Input
                  id="firstName"
                  name="firstName"
                  type="text"
                  placeholder="John"
                  value={formData.firstName}
                  onChange={handleChange}
                  required
                  className="h-12"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName" className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                  Last Name
                </Label>
                <Input
                  id="lastName"
                  name="lastName"
                  type="text"
                  placeholder="Doe"
                  value={formData.lastName}
                  onChange={handleChange}
                  required
                  className="h-12"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="email" className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                Email
              </Label>
              <Input
                id="email"
                name="email"
                type="email"
                placeholder="you@example.com"
                value={formData.email}
                onChange={handleChange}
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
                name="password"
                type="password"
                placeholder="At least 6 characters"
                value={formData.password}
                onChange={handleChange}
                required
                className="h-12"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirmPassword" className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                Confirm Password
              </Label>
              <Input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                placeholder="Confirm your password"
                value={formData.confirmPassword}
                onChange={handleChange}
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
              {isLoading ? "Creating account..." : "Create Account"}
            </Button>
          </form>

          <div className="text-center text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
            <span className="text-muted-foreground">Already have an account? </span>
            <Link href="/login" className="text-foreground hover:underline font-medium">
              Sign in
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}






