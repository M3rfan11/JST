"use client"

import { useState, useEffect } from "react"
import { useRouter, useParams } from "next/navigation"
import { ArrowLeft, Save, Upload, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { api } from "@/lib/api-client"
import { useToast } from "@/hooks/use-toast"
import Link from "next/link"

export default function EditCategoryPage() {
  const router = useRouter()
  const params = useParams()
  const categoryId = parseInt(params.id as string)
  const { toast } = useToast()
  const [loading, setLoading] = useState(false)
  const [loadingCategory, setLoadingCategory] = useState(true)
  const [uploadedImages, setUploadedImages] = useState<string[]>([]) // Base64 data URLs from file uploads
  
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    imageUrls: "",
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

  useEffect(() => {
    loadCategory()
  }, [categoryId])

  const loadCategory = async () => {
    try {
      setLoadingCategory(true)
      const category: any = await api.categories.getById(categoryId)
      
      // Handle both camelCase and PascalCase property names
      const imageUrls = category.imageUrls || category.ImageUrls
      
      console.log("Loaded category:", {
        id: category.id,
        name: category.name,
        hasImageUrls: !!imageUrls,
        imageUrlsPreview: imageUrls ? (imageUrls.substring(0, 100) + "...") : null,
        imageUrlsLength: imageUrls?.length || 0,
        rawCategory: category
      })
      
      // Parse existing images - separate base64 data URLs from regular URLs
      const existingImages: string[] = []
      let imageUrlsDisplay = ""
      
      if (imageUrls) {
        try {
          const parsed = JSON.parse(imageUrls)
          console.log("Parsed imageUrls:", {
            isArray: Array.isArray(parsed),
            length: Array.isArray(parsed) ? parsed.length : 0,
            firstItem: Array.isArray(parsed) && parsed.length > 0 ? parsed[0].substring(0, 50) + "..." : null
          })
          
          if (Array.isArray(parsed)) {
            // Separate base64 data URLs from regular URLs
            parsed.forEach((url: string, index: number) => {
              if (typeof url === 'string' && url.startsWith('data:image/')) {
                existingImages.push(url)
                console.log(`Found base64 image ${index + 1}, length: ${url.length}`)
              } else if (typeof url === 'string' && url.length > 0) {
                // Regular URL - add to textarea for editing
                if (imageUrlsDisplay) {
                  imageUrlsDisplay += '\n' + url
                } else {
                  imageUrlsDisplay = url
                }
                console.log(`Found regular URL ${index + 1}: ${url.substring(0, 50)}`)
              }
            })
          } else {
            // Not an array, treat as single string
            if (typeof parsed === 'string' && parsed.startsWith('data:image/')) {
              existingImages.push(parsed)
            } else {
              imageUrlsDisplay = category.imageUrls
            }
          }
        } catch (parseError) {
          console.error("Error parsing imageUrls:", parseError)
          console.log("Raw imageUrls value:", imageUrls?.substring(0, 200))
          // If parsing fails, try to treat as single image
          if (imageUrls && typeof imageUrls === 'string' && imageUrls.startsWith('data:image/')) {
            existingImages.push(imageUrls)
          } else if (imageUrls) {
            imageUrlsDisplay = imageUrls
          }
        }
      }
      
      console.log("Processed images:", {
        base64Count: existingImages.length,
        urlCount: imageUrlsDisplay ? imageUrlsDisplay.split('\n').filter(u => u.trim()).length : 0
      })
      
      setUploadedImages(existingImages)
      
      setFormData({
        name: category.name || "",
        description: category.description || "",
        imageUrls: imageUrlsDisplay,
        isActive: category.isActive !== undefined ? category.isActive : true,
      })
    } catch (error: any) {
      console.error("Error loading category:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to load category.",
        variant: "destructive",
      })
      router.push("/admin/categories")
    } finally {
      setLoadingCategory(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!formData.name.trim()) {
      toast({
        title: "Validation Error",
        description: "Category name is required.",
        variant: "destructive",
      })
      return
    }

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
      
      console.log("Updating category with data:", {
        name: formData.name,
        imageUrlsLength: imageUrlsJson?.length || 0,
        imageCount: allImages.length
      })

      const updateData = {
        name: formData.name.trim(),
        description: formData.description.trim() || null,
        imageUrls: imageUrlsJson,
        isActive: formData.isActive,
      }

      console.log("Updating category with data:", JSON.stringify(updateData, null, 2))

      await api.categories.update(categoryId, updateData)
      
      toast({
        title: "Success!",
        description: "Category updated successfully.",
      })
      
      router.push("/admin/categories")
    } catch (error: any) {
      console.error("Error updating category:", error)
      console.error("Error details:", error.data || error)
      
      let errorMessage = "Failed to update category. Please try again."
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

  if (loadingCategory) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading category...</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen" style={{ backgroundColor: 'rgba(206, 180, 157, 1)' }}>
      <div className="max-w-4xl mx-auto p-6">
        <div className="bg-white rounded-lg shadow-lg">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b">
            <h1 className="text-2xl font-semibold" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Edit Category
            </h1>
            <Button variant="ghost" size="icon" asChild>
              <Link href="/admin/categories">
                <ArrowLeft className="h-5 w-5" />
              </Link>
            </Button>
          </div>

          <form onSubmit={handleSubmit} className="p-6 space-y-6">
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="name" style={{ fontFamily: '"Dream Avenue"' }}>Name *</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="Category name"
                  required
                  style={{ fontFamily: '"Dream Avenue"' }}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description" style={{ fontFamily: '"Dream Avenue"' }}>Description</Label>
                <textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Category description"
                  rows={6}
                  className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background resize-y"
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
                {uploadedImages.length > 0 ? (
                  <div>
                    <p className="text-sm text-muted-foreground mb-2" style={{ fontFamily: '"Dream Avenue"' }}>
                      {uploadedImages.length} {uploadedImages.length === 1 ? 'image' : 'images'} loaded
                    </p>
                    <div className="grid grid-cols-4 gap-4">
                      {uploadedImages.map((img, index) => (
                        <div key={index} className="relative group">
                          <img
                            src={img}
                            alt={`Uploaded image ${index + 1}`}
                            className="w-full h-32 object-cover rounded-md border"
                            onError={(e) => {
                              console.error(`Error loading image ${index + 1}:`, img.substring(0, 100))
                              e.currentTarget.style.display = 'none'
                            }}
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
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    No images loaded. Upload images or enter URLs above.
                  </p>
                )}

                {/* Image URL Input */}
                <div className="space-y-2">
                  <Label htmlFor="imageUrls" style={{ fontFamily: '"Dream Avenue"' }}>Or enter image URLs</Label>
                  <textarea
                    id="imageUrls"
                    value={formData.imageUrls}
                    onChange={(e) => setFormData({ ...formData, imageUrls: e.target.value })}
                    placeholder='Enter image URLs as JSON array: ["/image1.jpg", "/image2.jpg"] or one URL per line'
                    rows={3}
                    className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background resize-y font-mono text-xs"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>
              </div>

              {/* Category Status Toggle */}
              <div className="flex items-center justify-between p-4 border rounded-md">
                <div>
                  <Label style={{ fontFamily: '"Dream Avenue"', fontWeight: 'bold' }}>Category Status</Label>
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    Active categories appear to customers, Inactive categories are hidden
                  </p>
                </div>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-500"></div>
                  {formData.isActive && (
                    <span className="ml-3 text-sm text-green-600" style={{ fontFamily: '"Dream Avenue"' }}>
                      ✓ Active
                    </span>
                  )}
                </label>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex items-center justify-end gap-4 pt-4 border-t">
              <Button
                type="button"
                variant="outline"
                onClick={() => router.back()}
                className="bg-white"
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
                {loading ? "Updating..." : "Update Category"}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

