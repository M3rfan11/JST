"use client"

import type React from "react"

import { useState, useEffect } from "react"
import { Button } from "./ui/button"
import { Input } from "./ui/input"
import { Label } from "./ui/label"
import { useToast } from "@/hooks/use-toast"

export function ProfileSettings() {
  const { toast } = useToast()
  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    email: "",
    phone: "",
  })

  useEffect(() => {
    const saved = localStorage.getItem("userProfile")
    if (saved) {
      setFormData(JSON.parse(saved))
    }
  }, [])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value })
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    localStorage.setItem("userProfile", JSON.stringify(formData))
    toast({
      title: "Profile updated",
      description: "Your changes have been saved successfully.",
    })
  }

  return (
    <div className="max-w-3xl">
      <h2 className="font-serif text-2xl sm:text-3xl font-semibold mb-8" style={{ fontFamily: '"Dream Avenue"' }}>Personal Information</h2>
      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
          <div className="space-y-2">
            <Label htmlFor="firstName" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
              First Name
            </Label>
            <Input
              id="firstName"
              name="firstName"
              value={formData.firstName}
              onChange={handleChange}
              required
              className="h-12"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="lastName" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
              Last Name
            </Label>
            <Input
              id="lastName"
              name="lastName"
              value={formData.lastName}
              onChange={handleChange}
              required
              className="h-12"
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="email" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
            Email
          </Label>
          <Input
            id="email"
            name="email"
            type="email"
            value={formData.email}
            onChange={handleChange}
            required
            className="h-12"
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="phone" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
            Phone
          </Label>
          <Input
            id="phone"
            name="phone"
            type="tel"
            value={formData.phone}
            onChange={handleChange}
            required
            className="h-12"
          />
        </div>

        <div className="border-t border-border pt-8 mt-8">
          <Button type="submit" size="lg" className="h-12 px-8 text-base" style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
            Save Changes
          </Button>
        </div>
      </form>

      <div className="mt-12 pt-12 border-t border-border">
        <h2 className="font-serif text-2xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Password</h2>
        <p className="text-muted-foreground mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Update your password to keep your account secure.</p>
        <Button variant="outline" size="lg" style={{ borderColor: '#3D0811', color: '#3D0811', fontFamily: '"Dream Avenue"' }}>
          Change Password
        </Button>
      </div>
    </div>
  )
}
