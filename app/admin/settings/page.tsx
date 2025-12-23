"use client"

import { useState, useEffect } from "react"
import { Save, Plus, X, Upload, Trash2, Image as ImageIcon } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { api } from "@/lib/api-client"
import { useToast } from "@/hooks/use-toast"
import Image from "next/image"

export default function SettingsPage() {
  const { toast } = useToast()
  const [messages, setMessages] = useState<string[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [qrCodeUrl, setQrCodeUrl] = useState<string | null>(null)
  const [qrFile, setQrFile] = useState<File | null>(null)
  const [uploadingQr, setUploadingQr] = useState(false)
  const [deletingQr, setDeletingQr] = useState(false)

  useEffect(() => {
    loadMessages()
    loadQrCode()
  }, [])

  const loadMessages = async () => {
    try {
      setLoading(true)
      const response = await api.settings.getShippingTicker() as { messages: string[] }
      if (response && response.messages) {
        setMessages(response.messages)
      } else {
        setMessages([
          "FREE SHIPPING ON ORDERS OVER 1000 EGP",
          "30-DAY RETURNS",
          "AUTHENTIC EGYPTIAN CRAFTSMANSHIP"
        ])
      }
    } catch (error: any) {
      console.error("Error loading shipping ticker messages:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to load shipping ticker messages.",
        variant: "destructive",
      })
      // Set default messages on error
      setMessages([
        "FREE SHIPPING ON ORDERS OVER 1000 EGP",
        "30-DAY RETURNS",
        "AUTHENTIC EGYPTIAN CRAFTSMANSHIP"
      ])
    } finally {
      setLoading(false)
    }
  }

  const handleSave = async () => {
    if (messages.length === 0) {
      toast({
        title: "Validation Error",
        description: "At least one message is required.",
        variant: "destructive",
      })
      return
    }

    // Validate all messages
    for (const message of messages) {
      if (!message.trim()) {
        toast({
          title: "Validation Error",
          description: "Messages cannot be empty.",
          variant: "destructive",
        })
        return
      }
      if (message.length > 200) {
        toast({
          title: "Validation Error",
          description: "Each message must be 200 characters or less.",
          variant: "destructive",
        })
        return
      }
    }

    try {
      setSaving(true)
      console.log("Saving shipping ticker messages:", messages)
      const response = await api.settings.updateShippingTicker(messages)
      console.log("Save response:", response)
      toast({
        title: "Success!",
        description: "Shipping ticker messages updated successfully.",
      })
    } catch (error: any) {
      console.error("Error saving shipping ticker messages:", error)
      console.error("Error details:", {
        message: error.message,
        status: error.status,
        data: error.data
      })
      toast({
        title: "Error",
        description: error.message || `Failed to save shipping ticker messages. ${error.status ? `Status: ${error.status}` : ''}`,
        variant: "destructive",
      })
    } finally {
      setSaving(false)
    }
  }

  const handleAddMessage = () => {
    setMessages([...messages, ""])
  }

  const handleRemoveMessage = (index: number) => {
    if (messages.length > 1) {
      setMessages(messages.filter((_, i) => i !== index))
    } else {
      toast({
        title: "Validation Error",
        description: "At least one message is required.",
        variant: "destructive",
      })
    }
  }

  const handleMessageChange = (index: number, value: string) => {
    const newMessages = [...messages]
    newMessages[index] = value
    setMessages(newMessages)
  }

  const loadQrCode = async () => {
    try {
      const response = await api.settings.getInstaPayQr() as { qrCodeUrl?: string | null }
      if (response && response.qrCodeUrl) {
        setQrCodeUrl(response.qrCodeUrl)
      }
    } catch (error: any) {
      console.error("Error loading QR code:", error)
    }
  }

  const handleQrFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0]
      // Validate file type
      if (!file.type.startsWith("image/")) {
        toast({
          title: "Invalid file type",
          description: "Please upload an image file (JPG, PNG, GIF, WEBP).",
          variant: "destructive",
        })
        return
      }
      // Validate file size (max 2MB)
      if (file.size > 2 * 1024 * 1024) {
        toast({
          title: "File too large",
          description: "Please upload a file smaller than 2MB.",
          variant: "destructive",
        })
        return
      }
      setQrFile(file)
    }
  }

  const handleUploadQr = async () => {
    if (!qrFile) {
      toast({
        title: "No file selected",
        description: "Please select a QR code image to upload.",
        variant: "destructive",
      })
      return
    }

    try {
      setUploadingQr(true)
      const formData = new FormData()
      formData.append("file", qrFile)
      
      const response = await api.settings.uploadInstaPayQr(formData) as { qrCodeUrl: string }
      setQrCodeUrl(response.qrCodeUrl)
      setQrFile(null)
      
      toast({
        title: "Success!",
        description: "InstaPay QR code uploaded successfully.",
      })
    } catch (error: any) {
      console.error("Error uploading QR code:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to upload QR code.",
        variant: "destructive",
      })
    } finally {
      setUploadingQr(false)
    }
  }

  const handleDeleteQr = async () => {
    if (!confirm("Are you sure you want to delete the InstaPay QR code?")) {
      return
    }

    try {
      setDeletingQr(true)
      await api.settings.deleteInstaPayQr()
      setQrCodeUrl(null)
      setQrFile(null)
      
      toast({
        title: "Success!",
        description: "InstaPay QR code deleted successfully.",
      })
    } catch (error: any) {
      console.error("Error deleting QR code:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to delete QR code.",
        variant: "destructive",
      })
    } finally {
      setDeletingQr(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading settings...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
          Settings
        </h1>
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          Manage website settings and configurations
        </p>
      </div>

      {/* Shipping Ticker Settings */}
      <div className="bg-white rounded-lg shadow-sm border border-border p-6">
        <div className="mb-6">
          <h2 className="text-xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            Shipping Ticker Messages
          </h2>
          <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
            Edit the messages displayed in the shipping ticker at the top of the website. These messages scroll continuously.
          </p>
        </div>

        <div className="space-y-4">
          {messages.map((message, index) => (
            <div key={index} className="flex gap-2">
              <Input
                value={message}
                onChange={(e) => handleMessageChange(index, e.target.value)}
                placeholder={`Message ${index + 1}`}
                maxLength={200}
                style={{ fontFamily: '"Dream Avenue"' }}
              />
              <Button
                type="button"
                variant="outline"
                size="icon"
                onClick={() => handleRemoveMessage(index)}
                disabled={messages.length === 1}
                className="flex-shrink-0"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}

          <Button
            type="button"
            variant="outline"
            onClick={handleAddMessage}
            className="w-full"
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            <Plus className="h-4 w-4 mr-2" />
            Add Message
          </Button>

          <div className="pt-4 border-t">
            <Button
              onClick={handleSave}
              disabled={saving}
              style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
            >
              <Save className="h-4 w-4 mr-2" />
              {saving ? "Saving..." : "Save Changes"}
            </Button>
          </div>
        </div>
      </div>

      {/* InstaPay QR Code Settings */}
      <div className="bg-white rounded-lg shadow-sm border border-border p-6">
        <div className="mb-6">
          <h2 className="text-xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            InstaPay QR Code
          </h2>
          <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
            Upload your InstaPay QR code image. This will be displayed on the payment page for customers to scan.
          </p>
        </div>

        <div className="space-y-4">
          {/* Current QR Code Display */}
          {qrCodeUrl && (
            <div className="border border-border rounded-lg p-4 bg-gray-50">
              <Label className="text-sm mb-2 block" style={{ fontFamily: '"Dream Avenue"' }}>
                Current QR Code:
              </Label>
              <div className="flex items-center gap-4">
                <div className="relative w-48 h-48 bg-white rounded-lg border border-border overflow-hidden">
                  <Image
                    src={`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081'}${qrCodeUrl}`}
                    alt="InstaPay QR Code"
                    fill
                    className="object-contain"
                    unoptimized
                  />
                </div>
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleDeleteQr}
                  disabled={deletingQr}
                  className="text-red-600 border-red-600 hover:bg-red-50"
                  style={{ fontFamily: '"Dream Avenue"' }}
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  {deletingQr ? "Deleting..." : "Delete"}
                </Button>
              </div>
            </div>
          )}

          {/* Upload New QR Code */}
          <div>
            <Label htmlFor="qrFile" className="text-sm mb-2 block" style={{ fontFamily: '"Dream Avenue"' }}>
              {qrCodeUrl ? "Replace QR Code:" : "Upload QR Code:"}
            </Label>
            <div className="space-y-3">
              <Input
                id="qrFile"
                type="file"
                accept="image/*"
                onChange={handleQrFileChange}
                className="cursor-pointer"
                disabled={uploadingQr}
              />
              <p className="text-xs text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                Accepted formats: JPG, PNG, GIF, WEBP (Max 2MB)
              </p>
              
              {qrFile && (
                <div className="bg-gray-50 rounded-lg p-3">
                  <p className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                    Selected: {qrFile.name} ({(qrFile.size / 1024).toFixed(2)} KB)
                  </p>
                </div>
              )}

              {qrFile && (
                <Button
                  type="button"
                  onClick={handleUploadQr}
                  disabled={uploadingQr}
                  style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
                >
                  <Upload className="h-4 w-4 mr-2" />
                  {uploadingQr ? "Uploading..." : "Upload QR Code"}
                </Button>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
