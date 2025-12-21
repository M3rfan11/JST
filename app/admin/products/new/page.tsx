"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { X, Upload, Plus as PlusIcon } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { api } from "@/lib/api-client"
import { useToast } from "@/hooks/use-toast"
import Link from "next/link"

interface Category {
  id: number
  name: string
}

interface VariantAttribute {
  name: string
  values: string[]
}

interface ProductVariant {
  attributes: Record<string, string>
  priceOverride?: number
  sku?: string
  imageUrl?: string
}

export default function NewProductPage() {
  const router = useRouter()
  const { toast } = useToast()
  const [loading, setLoading] = useState(false)
  const [categories, setCategories] = useState<Category[]>([])
  const [selectedCategories, setSelectedCategories] = useState<number[]>([])
  const [images, setImages] = useState<string[]>([])
  const [imageUrlInput, setImageUrlInput] = useState("")
  const [variantAttributes, setVariantAttributes] = useState<VariantAttribute[]>([])
  const [attributeInput, setAttributeInput] = useState("")
  const [generatedVariants, setGeneratedVariants] = useState<ProductVariant[]>([])
  
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    price: "0.00",
    sku: "",
    brand: "",
    washingInstructions: "",
    alwaysAvailable: false,
    isActive: true,
  })

  useEffect(() => {
    loadCategories()
  }, [])

  useEffect(() => {
    generateVariants()
  }, [variantAttributes])

  const loadCategories = async () => {
    try {
      const data = await api.categories.getAll() as Category[]
      setCategories(Array.isArray(data) ? data : [])
    } catch (error) {
      console.error("Error loading categories:", error)
    }
  }

  const handleCategoryToggle = (categoryId: number) => {
    setSelectedCategories(prev =>
      prev.includes(categoryId)
        ? prev.filter(id => id !== categoryId)
        : [...prev, categoryId]
    )
  }

  const handleImageUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (files) {
      // For now, we'll just store file names. In production, upload to server
      Array.from(files).forEach(file => {
        // Create a preview URL (in production, upload to server first)
        const reader = new FileReader()
        reader.onload = (e) => {
          if (e.target?.result) {
            setImages(prev => [...prev, e.target.result as string])
          }
        }
        reader.readAsDataURL(file)
      })
    }
  }

  const handleAddImageUrl = () => {
    if (imageUrlInput.trim()) {
      setImages(prev => [...prev, imageUrlInput.trim()])
      setImageUrlInput("")
    }
  }

  const handleRemoveImage = (index: number) => {
    setImages(prev => prev.filter((_, i) => i !== index))
  }

  const handleAddAttribute = () => {
    if (attributeInput.trim()) {
      setVariantAttributes(prev => [...prev, { name: attributeInput.trim(), values: [] }])
      setAttributeInput("")
    }
  }

  const handleAddAttributeValue = (attrIndex: number, value: string) => {
    if (value.trim()) {
      setVariantAttributes(prev => {
        const updated = [...prev]
        if (!updated[attrIndex].values.includes(value.trim())) {
          updated[attrIndex].values.push(value.trim())
        }
        return updated
      })
    }
  }

  const handleRemoveAttribute = (index: number) => {
    setVariantAttributes(prev => prev.filter((_, i) => i !== index))
  }

  const generateVariants = () => {
    if (variantAttributes.length === 0) {
      setGeneratedVariants([])
      return
    }

    // Generate all combinations of variant attributes
    const combinations: ProductVariant[] = []
    
    const generateCombinations = (attrs: VariantAttribute[], current: Record<string, string> = {}, index: number = 0) => {
      if (index === attrs.length) {
        combinations.push({ attributes: { ...current } })
        return
      }

      const attr = attrs[index]
      if (attr.values.length === 0) {
        generateCombinations(attrs, current, index + 1)
      } else {
        attr.values.forEach(value => {
          generateCombinations(attrs, { ...current, [attr.name]: value }, index + 1)
        })
      }
    }

    generateCombinations(variantAttributes)
    setGeneratedVariants(combinations)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    // Validate required fields
    if (!formData.name.trim()) {
      toast({
        title: "Validation Error",
        description: "Product name is required.",
        variant: "destructive",
      })
      return
    }

    if (selectedCategories.length === 0) {
      toast({
        title: "Validation Error",
        description: "Please select at least one category.",
        variant: "destructive",
      })
      return
    }

    if (!formData.price || parseFloat(formData.price) <= 0) {
      toast({
        title: "Validation Error",
        description: "Please enter a valid price.",
        variant: "destructive",
      })
      return
    }

    setLoading(true)

    try {
      // Build variant attributes JSON (just the attribute names)
      const variantAttributesJson = variantAttributes.length > 0
        ? JSON.stringify(variantAttributes.map(attr => attr.name))
        : null
      
      // Build variants if we have attributes and values
      const variants = generatedVariants.length > 0
        ? generatedVariants.map(variant => ({
            color: "", // Legacy field
            attributes: JSON.stringify(variant.attributes),
            priceOverride: variant.priceOverride || null,
            sku: variant.sku || null,
            imageUrl: variant.imageUrl || null,
            isActive: true,
          }))
        : null

      const productData = {
        name: formData.name.trim(),
        description: formData.description.trim() || null,
        price: parseFloat(formData.price) || 0,
        sku: formData.sku.trim() || null,
        brand: formData.brand.trim() || null,
        washingInstructions: formData.washingInstructions.trim() || null,
        categoryId: selectedCategories[0] || 0, // Primary category
        categoryIds: selectedCategories.length > 0 ? selectedCategories : null,
        isActive: formData.isActive,
        alwaysAvailable: formData.alwaysAvailable,
        inventoryTracked: !formData.alwaysAvailable,
        sellWhenOutOfStock: false,
        taxable: true,
        isPhysicalProduct: true,
        status: formData.isActive ? 1 : 0, // 0 = Draft, 1 = Active (ProductStatus enum)
        variantAttributes: variantAttributesJson, // Already a JSON string
        imageUrl: images[0] || null,
        mediaUrls: images.length > 0 ? JSON.stringify(images) : null,
        variants: variants,
      }

      console.log("Sending product data:", JSON.stringify(productData, null, 2))

      await api.products.create(productData)
      
      toast({
        title: "Success!",
        description: "Product created successfully.",
      })
      
      router.push("/admin/products")
    } catch (error: any) {
      console.error("Error creating product:", error)
      console.error("Error details:", error.data || error)
      
      // Extract detailed error message
      let errorMessage = "Failed to create product. Please try again."
      if (error.message) {
        errorMessage = error.message
      } else if (error.data) {
        if (typeof error.data === 'string') {
          errorMessage = error.data
        } else if (error.data.message) {
          errorMessage = error.data.message
        } else if (error.data.errors) {
          // Handle validation errors
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
    <div className="min-h-screen" style={{ backgroundColor: 'rgba(206, 180, 157, 1)' }}>
      <div className="max-w-6xl mx-auto p-6">
        <div className="bg-white rounded-lg shadow-lg">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b">
            <h1 className="text-2xl font-semibold" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Create Product
            </h1>
            <Button variant="ghost" size="icon" asChild>
              <Link href="/admin/products">
                <X className="h-5 w-5" />
              </Link>
            </Button>
          </div>

          <form onSubmit={handleSubmit} className="p-6 space-y-6">
            {/* Two Column Layout */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Left Column */}
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="name" style={{ fontFamily: '"Dream Avenue"' }}>Name</Label>
                  <Input
                    id="name"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    placeholder="Product name"
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
                    placeholder="Product description"
                    rows={6}
                    className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background resize-y"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="price" style={{ fontFamily: '"Dream Avenue"' }}>Price</Label>
                  <Input
                    id="price"
                    type="number"
                    step="0.01"
                    value={formData.price}
                    onChange={(e) => setFormData({ ...formData, price: e.target.value })}
                    placeholder="0.00"
                    required
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="sku" style={{ fontFamily: '"Dream Avenue"' }}>SKU</Label>
                  <Input
                    id="sku"
                    value={formData.sku}
                    onChange={(e) => setFormData({ ...formData, sku: e.target.value })}
                    placeholder="SKU"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>
              </div>

              {/* Right Column */}
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label style={{ fontFamily: '"Dream Avenue"' }}>Categories (Select multiple)</Label>
                  <div className="space-y-2 border rounded-md p-4 max-h-64 overflow-y-auto">
                    {categories.map((category) => (
                      <div key={category.id} className="flex items-center space-x-2">
                        <input
                          type="checkbox"
                          id={`category-${category.id}`}
                          checked={selectedCategories.includes(category.id)}
                          onChange={() => handleCategoryToggle(category.id)}
                          className="rounded border-gray-300"
                        />
                        <Label
                          htmlFor={`category-${category.id}`}
                          className="cursor-pointer"
                          style={{ fontFamily: '"Dream Avenue"' }}
                        >
                          {category.name}
                        </Label>
                      </div>
                    ))}
                    {categories.length === 0 && (
                      <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                        No categories available
                      </p>
                    )}
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="brand" style={{ fontFamily: '"Dream Avenue"' }}>Brand</Label>
                  <Input
                    id="brand"
                    value={formData.brand}
                    onChange={(e) => setFormData({ ...formData, brand: e.target.value })}
                    placeholder="Brand"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="washingInstructions" style={{ fontFamily: '"Dream Avenue"' }}>Washing Instructions (Optional)</Label>
                  <textarea
                    id="washingInstructions"
                    value={formData.washingInstructions}
                    onChange={(e) => setFormData({ ...formData, washingInstructions: e.target.value })}
                    placeholder="e.g., Machine wash cold, gentle cycle. Do not bleach. Tumble dry low."
                    rows={4}
                    className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background resize-y"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>
              </div>
            </div>

            {/* Product Images */}
            <div className="space-y-4">
              <div>
                <h3 className="text-lg font-semibold mb-1" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  Product Images (You can add multiple images)
                </h3>
                <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                  Upload images from your computer or add image URLs
                </p>
              </div>

              {/* Upload Area */}
              <div className="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center">
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
                  <Upload className="h-12 w-12 text-gray-400 mb-4" />
                  <p className="text-sm" style={{ fontFamily: '"Dream Avenue"' }}>
                    Click to upload images from your computer
                  </p>
                  <p className="text-xs text-muted-foreground mt-1" style={{ fontFamily: '"Dream Avenue"' }}>
                    PNG, JPG, GIF up to 10MB
                  </p>
                </label>
              </div>

              {/* Image URL Input */}
              <div className="flex gap-2">
                <Input
                  value={imageUrlInput}
                  onChange={(e) => setImageUrlInput(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddImageUrl())}
                  placeholder="Or enter image URL and press Enter or click +"
                  style={{ fontFamily: '"Dream Avenue"' }}
                />
                <Button
                  type="button"
                  onClick={handleAddImageUrl}
                  style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
                >
                  <PlusIcon className="h-4 w-4" />
                </Button>
              </div>

              {/* Image Preview */}
              {images.length > 0 && (
                <div className="grid grid-cols-4 gap-4">
                  {images.map((img, index) => (
                    <div key={index} className="relative group">
                      <img
                        src={img}
                        alt={`Product image ${index + 1}`}
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
            </div>

            {/* Variant Definitions */}
            <div className="space-y-4">
              <div>
                <h3 className="text-lg font-semibold mb-1" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  Variant Definitions
                </h3>
                <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                  Add attributes and their values. All combinations will be automatically generated.
                </p>
              </div>

              <div className="flex gap-2">
                <Input
                  value={attributeInput}
                  onChange={(e) => setAttributeInput(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddAttribute())}
                  placeholder="Attribute name (e.g., Color, Size, Parts)"
                  style={{ fontFamily: '"Dream Avenue"' }}
                />
                <Button
                  type="button"
                  onClick={handleAddAttribute}
                  style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
                >
                  + Add
                </Button>
              </div>

              {/* Attribute List */}
              {variantAttributes.map((attr, attrIndex) => (
                <div key={attrIndex} className="border rounded-md p-4 space-y-2">
                  <div className="flex items-center justify-between">
                    <Label style={{ fontFamily: '"Dream Avenue"', fontWeight: 'bold' }}>{attr.name}</Label>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => handleRemoveAttribute(attrIndex)}
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {attr.values.map((value, valueIndex) => (
                      <span
                        key={valueIndex}
                        className="px-3 py-1 bg-gray-100 rounded-md text-sm"
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {value}
                      </span>
                    ))}
                    <Input
                      placeholder="Add value"
                      className="w-32"
                      onKeyPress={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault()
                          handleAddAttributeValue(attrIndex, e.currentTarget.value)
                          e.currentTarget.value = ''
                        }
                      }}
                      style={{ fontFamily: '"Dream Avenue"' }}
                    />
                  </div>
                </div>
              ))}
            </div>

            {/* Product Variants */}
            <div className="space-y-4">
              <div>
                <h3 className="text-lg font-semibold mb-1" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  Product Variants
                </h3>
                <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                  Configure variant attributes above to automatically generate variants.
                </p>
              </div>
              {generatedVariants.length > 0 ? (
                <div className="border rounded-md p-4 bg-gray-50">
                  <p className="text-sm font-medium mb-2" style={{ fontFamily: '"Dream Avenue"' }}>
                    {generatedVariants.length} variant(s) will be created:
                  </p>
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
                    {generatedVariants.slice(0, 12).map((variant, index) => (
                      <div key={index} className="text-xs p-2 bg-white rounded border" style={{ fontFamily: '"Dream Avenue"' }}>
                        {Object.entries(variant.attributes).map(([key, value]) => (
                          <div key={key}>{key}: {value}</div>
                        ))}
                      </div>
                    ))}
                  </div>
                  {generatedVariants.length > 12 && (
                    <p className="text-xs text-muted-foreground mt-2" style={{ fontFamily: '"Dream Avenue"' }}>
                      ... and {generatedVariants.length - 12} more
                    </p>
                  )}
                </div>
              ) : variantAttributes.length > 0 && variantAttributes.some(attr => attr.values.length === 0) ? (
                <div className="border rounded-md p-4 bg-yellow-50">
                  <p className="text-sm text-yellow-800" style={{ fontFamily: '"Dream Avenue"' }}>
                    Add values to variant attributes to generate variants.
                  </p>
                </div>
              ) : (
                <div className="border rounded-md p-4 bg-gray-50">
                  <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    No variants defined. Product will be created without variants.
                  </p>
                </div>
              )}
            </div>

            {/* Always Available Toggle */}
            <div className="flex items-center justify-between p-4 border rounded-md">
              <div>
                <Label style={{ fontFamily: '"Dream Avenue"', fontWeight: 'bold' }}>Always Available</Label>
                <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                  Product is always available regardless of stock level
                </p>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={formData.alwaysAvailable}
                  onChange={(e) => setFormData({ ...formData, alwaysAvailable: e.target.checked })}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-500"></div>
              </label>
            </div>

            {/* Product Status Toggle */}
            <div className="flex items-center justify-between p-4 border rounded-md">
              <div>
                <Label style={{ fontFamily: '"Dream Avenue"', fontWeight: 'bold' }}>Product Status</Label>
                <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                  Active products appear to customers, Draft products are hidden
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
                {loading ? "Creating..." : "Create Product"}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}
