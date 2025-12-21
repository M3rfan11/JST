"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, Save, Upload, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { api } from "@/lib/api-client"
import { useToast } from "@/hooks/use-toast"
import Link from "next/link"

export default function NewCategoryPage() {
  const router = useRouter()
  const { toast } = useToast()
  const [loading, setLoading] = useState(false)
  const [uploadedImages, setUploadedImages] = useState<string[]>([]) // Base64 data URLs from file uploads
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    imageUrls: "", // JSON array of image URLs (e.g., ["/image1.jpg", "/image2.jpg"])
    isActive: true,
  })

  const handleImageUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (files) {
      Array.from(files).forEach(file => {
        // Validate file type
        if (!file.type.startsWith('image/')) {
          toast({
            title: "Invalid file type",
            description: "Please upload image files only.",
            variant: "destructive",
          })
          return
        }
        
        // Validate file size (max 10MB)
        if (file.size > 10 * 1024 * 1024) {
          toast({
            title: "File too large",
            description: "Please upload images smaller than 10MB.",
            variant: "destructive",
          })
          return
        }

        // Create a preview URL (base64 data URL)
        const reader = new FileReader()
        reader.onload = (e) => {
          if (e.target?.result) {
            setUploadedImages(prev => [...prev, e.target.result as string])
          }
        }
        reader.readAsDataURL(file)
      })
    }
  }

  const handleRemoveImage = (index: number) => {
    setUploadedImages(prev => prev.filter((_, i) => i !== index))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)

    try {
      // Combine uploaded images (base64) and URL inputs
      const allImages: string[] = [...uploadedImages]
      
      // Parse imageUrls - if it's a JSON array string, use it; if it's line-separated URLs, convert to JSON array
      if (formData.imageUrls.trim()) {
        try {
          // Try to parse as JSON first
          const parsed = JSON.parse(formData.imageUrls.trim())
          if (Array.isArray(parsed)) {
            allImages.push(...parsed)
          }
        } catch {
          // If not valid JSON, treat as line-separated URLs
          const urls = formData.imageUrls
            .split('\n')
            .map(url => url.trim())
            .filter(url => url.length > 0)
          allImages.push(...urls)
        }
      }

      const imageUrlsJson = allImages.length > 0 ? JSON.stringify(allImages) : null
      
      // Warn if images are very large (but allow them - SQLite can handle large TEXT fields)
      if (imageUrlsJson && imageUrlsJson.length > 1000000) {
        toast({
          title: "Large image data",
          description: `Total image data is ${(imageUrlsJson.length / 1024 / 1024).toFixed(2)}MB. This may take a moment to save.`,
        })
      }

      console.log("Creating category with data:", {
        name: formData.name,
        imageUrlsLength: imageUrlsJson?.length || 0,
        imageCount: allImages.length,
        hasBase64Images: uploadedImages.length > 0
      })

      // Warn if images are very large
      if (imageUrlsJson && imageUrlsJson.length > 500000) {
        const shouldContinue = confirm(
          `The image data is very large (${(imageUrlsJson.length / 1024 / 1024).toFixed(2)}MB). ` +
          `This may take a while to save. Continue?`
        )
        if (!shouldContinue) {
          setLoading(false)
          return
        }
      }

      const categoryData = {
        name: formData.name,
        description: formData.description || null,
        imageUrls: imageUrlsJson,
        isActive: formData.isActive,
      }

      console.log("Sending category data to API...")
      const response = await api.categories.create(categoryData)
      console.log("Category created successfully:", response)
      
      toast({
        title: "Success!",
        description: "Category created successfully.",
      })
      
      router.push("/admin/categories")
    } catch (error: any) {
      console.error("Error creating category:", error)
      console.error("Error details:", error.data || error)
      
      let errorMessage = "Failed to create category. Please try again."
      if (error.message) {
        errorMessage = error.message
      } else if (error.data) {
        if (typeof error.data === 'string') {
          errorMessage = error.data
        } else if (error.data.message) {
          errorMessage = error.data.message
        } else if (error.data.errors) {
          const validationErrors = Object.entries(error.data.errors)
            .map(([field, messages]: [string, any]) => `${field}: ${Array.isArray(messages) ? messages.join(', ') : messages}`)
            .join('\n')
          errorMessage = validationErrors || errorMessage
        }
      }
      
      toast({
        title: "Error",
        description: errorMessage,
        variant: "destructive",
      })
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" asChild>
          <Link href="/admin/categories">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back
          </Link>
        </Button>
        <div>
          <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            Add New Category
          </h1>
          <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
            Create a new product category
          </p>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="bg-white rounded-lg border border-border p-6 space-y-6">
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name" style={{ fontFamily: '"Dream Avenue"' }}>Category Name *</Label>
            <Input
              id="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
              placeholder="e.g., Suits, Blazers, Shirts"
              style={{ fontFamily: '"Dream Avenue"' }}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="description" style={{ fontFamily: '"Dream Avenue"' }}>Description</Label>
            <textarea
              id="description"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              rows={4}
              placeholder="Describe this category..."
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              style={{ fontFamily: '"Dream Avenue"' }}
            />
          </div>

          <div className="space-y-4">
            <div>
              <Label style={{ fontFamily: '"Dream Avenue"' }}>Collection Images (Optional)</Label>
              <p className="text-sm text-muted-foreground mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
                Upload images from your computer or add image URLs. First image will be used as the primary collection image.
              </p>
            </div>

            {/* Upload Area */}
            <div className="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center">
              <input
                type="file"
                id="image-upload"
                multiple
                accept="image/*"
                onChange={handleImageUpload}
                className="hidden"
              />
              <label
                htmlFor="image-upload"
                className="cursor-pointer flex flex-col items-center"
              >
                <Upload className="h-10 w-10 text-gray-400 mb-3" />
                <p className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                  Click to upload images from your computer
                </p>
                <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                  PNG, JPG, GIF up to 10MB
                </p>
              </label>
            </div>

            {/* Image Previews */}
            {uploadedImages.length > 0 && (
              <div className="grid grid-cols-4 gap-4">
                {uploadedImages.map((img, index) => (
                  <div key={index} className="relative group">
                    <img
                      src={img}
                      alt={`Uploaded image ${index + 1}`}
                      className="w-full h-32 object-cover rounded-md border"
                    />
                    <button
                      type="button"
                      onClick={() => handleRemoveImage(index)}
                      className="absolute top-2 right-2 bg-red-500 text-white rounded-full p-1 opacity-0 group-hover:opacity-100 transition-opacity"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </div>
                ))}
              </div>
            )}

            {/* Image URL Input */}
            <div className="space-y-2">
              <Label htmlFor="imageUrls" style={{ fontFamily: '"Dream Avenue"' }}>Or enter image URLs</Label>
              <textarea
                id="imageUrls"
                value={formData.imageUrls}
                onChange={(e) => setFormData({ ...formData, imageUrls: e.target.value })}
                rows={3}
                placeholder='Enter image URLs as JSON array: ["/image1.jpg", "/image2.jpg"] or one URL per line'
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background font-mono text-xs"
                style={{ fontFamily: '"Dream Avenue"' }}
              />
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <input
              type="checkbox"
              id="isActive"
              checked={formData.isActive}
              onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
              className="rounded border-gray-300"
            />
            <Label htmlFor="isActive" style={{ fontFamily: '"Dream Avenue"' }}>Active (visible to customers)</Label>
          </div>
        </div>

        <div className="flex items-center justify-end gap-4 pt-4 border-t">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.back()}
            style={{ fontFamily: '"Dream Avenue"' }}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={loading}
            style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
          >
            <Save className="h-4 w-4 mr-2" />
            {loading ? "Creating..." : "Create Category"}
          </Button>
        </div>
      </form>
    </div>
  )
}





