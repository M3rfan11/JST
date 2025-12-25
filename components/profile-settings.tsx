"use client"

import type React from "react"
import { useState, useEffect } from "react"
import { Button } from "./ui/button"
import { Input } from "./ui/input"
import { Label } from "./ui/label"
import { useToast } from "@/hooks/use-toast"
import { api } from "@/lib/api-client"


export function ProfileSettings() {
  const { toast } = useToast()
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [changingPassword, setChangingPassword] = useState(false)
  
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
  })

  const [passwordData, setPasswordData] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  })

  useEffect(() => {
    loadProfile()
  }, [])

  const loadProfile = async () => {
    try {
      setLoading(true)
      const data: any = await api.users.getProfile()
      setFormData({
        fullName: data.fullName || "",
        email: data.email || "",
      })
    } catch (error) {
      console.error("Error loading profile:", error)
      toast({
        title: "Error",
        description: "Failed to load profile data",
        variant: "destructive",
      })
    } finally {
      setLoading(false)
    }
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value })
  }

  const handlePasswordChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setPasswordData({ ...passwordData, [e.target.name]: e.target.value })
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      setSaving(true)
      await api.users.updateProfile(formData)
      toast({
        title: "Profile updated",
        description: "Your changes have been saved successfully.",
      })
    } catch (error) {
      console.error("Error updating profile:", error)
      toast({
        title: "Error",
        description: "Failed to update profile",
        variant: "destructive",
      })
    } finally {
      setSaving(false)
    }
  }

  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      toast({
        title: "Validation Error",
        description: "New passwords do not match",
        variant: "destructive",
      })
      return
    }

    if (passwordData.newPassword.length < 6) {
      toast({
        title: "Validation Error",
        description: "Password must be at least 6 characters long",
        variant: "destructive",
      })
      return
    }

    try {
      setChangingPassword(true)
      await api.auth.changePassword({
        currentPassword: passwordData.currentPassword,
        newPassword: passwordData.newPassword
      })
      
      toast({
        title: "Password Changed",
        description: "Your password has been updated successfully.",
      })
      
      setPasswordData({
        currentPassword: "",
        newPassword: "",
        confirmPassword: "",
      })
    } catch (error: any) {
      console.error("Error changing password:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to change password",
        variant: "destructive",
      })
    } finally {
      setChangingPassword(false)
    }
  }

  if (loading) {
    return (
      <div className="flex justify-center p-8">
        <p style={{ fontFamily: '"Dream Avenue"' }}>Loading profile...</p>
      </div>
    )
  }

  return (
    <div className="max-w-3xl">
      <h2 className="font-serif text-2xl sm:text-3xl font-semibold mb-8" style={{ fontFamily: '"Dream Avenue"' }}>Personal Information</h2>
      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="space-y-2">
          <Label htmlFor="fullName" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
            Full Name
          </Label>
          <Input
            id="fullName"
            name="fullName"
            value={formData.fullName}
            onChange={handleChange}
            required
            className="h-12"
          />
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

        <div className="border-t border-border pt-8 mt-8">
          <Button 
            type="submit" 
            size="lg" 
            disabled={saving}
            className="h-12 px-8 text-base" 
            style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
          >
            {saving ? "Saving..." : "Save Changes"}
          </Button>
        </div>
      </form>

      <div className="mt-12 pt-12 border-t border-border">
        <h2 className="font-serif text-2xl font-semibold mb-8" style={{ fontFamily: '"Dream Avenue"' }}>Change Password</h2>
        <form onSubmit={handlePasswordSubmit} className="space-y-6">
          <div className="space-y-2">
            <Label htmlFor="currentPassword" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
              Current Password
            </Label>
            <Input
              id="currentPassword"
              name="currentPassword"
              type="password"
              value={passwordData.currentPassword}
              onChange={handlePasswordChange}
              required
              className="h-12"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="newPassword" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
              New Password
            </Label>
            <Input
              id="newPassword"
              name="newPassword"
              type="password"
              value={passwordData.newPassword}
              onChange={handlePasswordChange}
              required
              className="h-12"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="confirmPassword" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
              Confirm New Password
            </Label>
            <Input
              id="confirmPassword"
              name="confirmPassword"
              type="password"
              value={passwordData.confirmPassword}
              onChange={handlePasswordChange}
              required
              className="h-12"
            />
          </div>

          <div className="mt-8">
            <Button 
              type="submit" 
              size="lg" 
              variant="outline"
              disabled={changingPassword}
              className="h-12 px-8 text-base"
              style={{ borderColor: '#3D0811', color: '#3D0811', fontFamily: '"Dream Avenue"' }}
            >
              {changingPassword ? "Updating..." : "Update Password"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
