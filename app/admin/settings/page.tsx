"use client"

import { useState, useEffect } from "react"
import { Save, Plus, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { api } from "@/lib/api-client"
import { useToast } from "@/hooks/use-toast"

export default function SettingsPage() {
  const { toast } = useToast()
  const [messages, setMessages] = useState<string[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    loadMessages()
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
    </div>
  )
}
