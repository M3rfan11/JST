"use client"

import { useState, useEffect } from "react"
import { Package } from "lucide-react"
import { api, apiClient } from "@/lib/api-client"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { useToast } from "@/hooks/use-toast"
import Image from "next/image"

interface Product {
  id: number
  name: string
  imageUrl: string | null
  categoryName: string
  variants: ProductVariant[]
  hasVariants: boolean
  productInventory: ProductInventory | null
}

interface ProductVariant {
  id: number
  attributes: Record<string, string> | string // Can be object or JSON string
  imageUrl: string | null
  variantInventory: VariantInventory | null
}

interface ProductInventory {
  id: number
  quantity: number
  unit: string | null
  warehouseId: number
}

interface VariantInventory {
  id: number
  quantity: number
  unit: string | null
  warehouseId: number
}

export default function InventoryPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [onlineWarehouseId, setOnlineWarehouseId] = useState<number | null>(null)
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null)
  const [selectedVariant, setSelectedVariant] = useState<ProductVariant | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [stockQuantity, setStockQuantity] = useState("")
  const [updating, setUpdating] = useState(false)
  const { toast } = useToast()

  useEffect(() => {
    loadInventory()
  }, [])

  const getOnlineWarehouseId = async (): Promise<number | null> => {
    try {
      const warehouses = await apiClient.get<any[]>('/api/warehouse')
      const onlineWarehouse = warehouses?.find((w: any) => w.name === "Online Store" || w.name?.toLowerCase().includes("online"))
      return onlineWarehouse?.id || warehouses?.[0]?.id || null
    } catch (error) {
      console.error("Error fetching warehouses:", error)
      return null
    }
  }

  const loadInventory = async () => {
    try {
      setLoading(true)
      
      // Get Online Store warehouse ID
      const warehouseId = await getOnlineWarehouseId()
      if (!warehouseId) {
        toast({
          title: "Error",
          description: "Online Store warehouse not found",
          variant: "destructive",
        })
        return
      }
      setOnlineWarehouseId(warehouseId)

      // Get all products
      const allProducts = await api.products.getAll() as any[]
      
      // Get all product inventories for Online Store warehouse
      const productInventories = await api.productInventory.getAll() as any[]
      const onlineProductInventories = productInventories.filter((pi: any) => pi.warehouseId === warehouseId)
      
      // Get all variants for products that have them
      const productsWithData: Product[] = []
      
      for (const product of allProducts) {
        const productInventory = onlineProductInventories.find((pi: any) => pi.productId === product.id)
        
        // Get variants if product has them
        let variants: ProductVariant[] = []
        
        // Debug logging for specific product
        if (product.name && product.name.includes("JST Classic Knit Pullover")) {
          console.log("🔍 Product variants check:", {
            productId: product.id,
            productName: product.name,
            variantsFromAPI: product.variants,
            variantsLength: product.variants?.length || 0,
            variantsArray: Array.isArray(product.variants),
            allVariants: product.variants,
          })
        }
        
        // Always try to fetch variants directly from API to ensure we get all variants
        // The product.variants might be filtered or incomplete
        let productVariants: any[] = []
        if (product.id) {
          try {
            const directVariants = await api.productVariants.getByProduct(product.id) as any[]
            if (directVariants && Array.isArray(directVariants) && directVariants.length > 0) {
              productVariants = directVariants
              if (product.name && product.name.includes("JST Classic Knit Pullover")) {
                console.log("📦 Fetched variants directly from API:", directVariants)
              }
            } else if (product.variants && Array.isArray(product.variants) && product.variants.length > 0) {
              // Fallback to product.variants if direct fetch returns nothing
              productVariants = product.variants
              if (product.name && product.name.includes("JST Classic Knit Pullover")) {
                console.log("📦 Using variants from product response:", product.variants)
              }
            }
          } catch (error) {
            console.error(`Error fetching variants for product ${product.id}:`, error)
            // Fallback to product.variants if API call fails
            if (product.variants && Array.isArray(product.variants)) {
              productVariants = product.variants
            }
          }
        } else if (product.variants && Array.isArray(product.variants)) {
          productVariants = product.variants
        }
        
        if (productVariants && Array.isArray(productVariants) && productVariants.length > 0) {
          // For inventory management, show ALL variants (both active and inactive)
          // so admins can manage stock for all variants
          // Filter to only active variants for display (backend returns all variants, we filter here)
          // Default to active if isActive is undefined
          const variantsToShow = productVariants.map((v: any) => {
            // Check both isActive (camelCase) and IsActive (PascalCase from backend)
            // Default to true (active) if undefined
            const isActiveValue = v.isActive !== undefined ? v.isActive : (v.IsActive !== undefined ? v.IsActive : true)
            const isActive = isActiveValue !== false
            
            if (product.name && product.name.includes("JST Classic Knit Pullover")) {
              console.log(`  Variant ${v.id}:`, {
                isActive: v.isActive,
                IsActive: v.IsActive,
                isActiveValue,
                willInclude: isActive,
                attributes: v.attributes,
                color: v.color,
              })
            }
            return { ...v, isActive }
          })
          
          if (product.name && product.name.includes("JST Classic Knit Pullover")) {
            console.log("✅ Variants to show:", {
              total: productVariants.length,
              variants: variantsToShow,
            })
          }
          
          // Get variant inventories for each variant in Online Store warehouse
          for (const variant of variantsToShow) {
            try {
              const variantInventories = await api.variantInventory.getByVariant(variant.id) as any[]
              // Filter for Online Store warehouse
              const onlineVariantInventory = variantInventories.find((vi: any) => vi.warehouseId === warehouseId)
              
              // Parse variant attributes if it's a JSON string
              let attributes: Record<string, string> = {}
              if (variant.attributes) {
                if (typeof variant.attributes === 'string') {
                  try {
                    const parsed = JSON.parse(variant.attributes)
                    // If it's an object, use it directly
                    if (typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)) {
                      attributes = parsed
                    } else {
                      attributes = {}
                    }
                  } catch {
                    // If parsing fails, attributes remain empty
                    attributes = {}
                  }
                } else if (typeof variant.attributes === 'object' && variant.attributes !== null) {
                  attributes = variant.attributes
                }
              }
              
              variants.push({
                id: variant.id,
                attributes,
                imageUrl: variant.imageUrl || variant.imageUrl,
                variantInventory: onlineVariantInventory ? {
                  id: onlineVariantInventory.id,
                  quantity: onlineVariantInventory.quantity,
                  unit: onlineVariantInventory.unit,
                  warehouseId: onlineVariantInventory.warehouseId,
                } : null,
              })
            } catch (error) {
              console.error(`Error loading inventory for variant ${variant.id}:`, error)
              // Still add variant even if inventory fetch fails
              let attributes: Record<string, string> = {}
              if (variant.attributes) {
                if (typeof variant.attributes === 'string') {
                  try {
                    const parsed = JSON.parse(variant.attributes)
                    if (typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)) {
                      attributes = parsed
                    }
                  } catch {
                    attributes = {}
                  }
                } else if (typeof variant.attributes === 'object' && variant.attributes !== null) {
                  attributes = variant.attributes
                }
              }
              variants.push({
                id: variant.id,
                attributes,
                imageUrl: variant.imageUrl || variant.imageUrl,
                variantInventory: null,
              })
            }
          }
        }
        
        // Debug: Log if product should have variants but doesn't
        if (product.name && product.name.includes("JST Classic Knit Pullover") && product.name.includes("Black")) {
          console.log("🔴 Black product final state:", {
            productId: product.id,
            productName: product.name,
            variantsFound: variants.length,
            productVariantsFromAPI: productVariants.length,
            hasVariants: variants.length > 0,
            variants: variants,
          })
        }
        
        productsWithData.push({
          id: product.id,
          name: product.name,
          imageUrl: product.imageUrl,
          categoryName: product.categoryName || "UNCATEGORIZED",
          variants,
          hasVariants: variants.length > 0,
          productInventory: productInventory ? {
            id: productInventory.id,
            quantity: productInventory.quantity,
            unit: productInventory.unit,
            warehouseId: productInventory.warehouseId,
          } : null,
        })
      }
      
      setProducts(productsWithData)
    } catch (error) {
      console.error("Error loading inventory:", error)
      toast({
        title: "Error",
        description: "Failed to load inventory data",
        variant: "destructive",
      })
    } finally {
      setLoading(false)
    }
  }

  const openUpdateDialog = (product: Product, variant?: ProductVariant) => {
    setSelectedProduct(product)
    setSelectedVariant(variant || null)
    
    if (variant && variant.variantInventory) {
      setStockQuantity(variant.variantInventory.quantity.toString())
    } else if (!variant && product.productInventory) {
      setStockQuantity(product.productInventory.quantity.toString())
    } else {
      setStockQuantity("0")
    }
    
    setIsDialogOpen(true)
  }

  const handleUpdateStock = async () => {
    if (!selectedProduct || !onlineWarehouseId) return
    
    const quantity = parseFloat(stockQuantity)
    if (isNaN(quantity) || quantity < 0) {
      toast({
        title: "Invalid Quantity",
        description: "Please enter a valid quantity",
        variant: "destructive",
      })
      return
    }

    try {
      setUpdating(true)
      
      if (selectedVariant) {
        // Update variant inventory for Online Store warehouse
        // Use the variant/warehouse endpoint which creates or updates
        await apiClient.put(`/api/variantinventory/variant/${selectedVariant.id}/warehouse/${onlineWarehouseId}`, {
          quantity,
          unit: selectedVariant.variantInventory?.unit || "piece",
        })
      } else {
        // Update product inventory
        if (selectedProduct.productInventory) {
          // Update existing
          await api.productInventory.update(selectedProduct.productInventory.id, {
            quantity,
            unit: selectedProduct.productInventory.unit || "piece",
          })
        } else {
          // Create new
          await api.productInventory.create({
            productId: selectedProduct.id,
            warehouseId: onlineWarehouseId,
            quantity,
            unit: "piece",
          })
        }
      }
      
      toast({
        title: "Success",
        description: "Stock updated successfully",
      })
      
      setIsDialogOpen(false)
      loadInventory()
    } catch (error: any) {
      console.error("Error updating stock:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to update stock",
        variant: "destructive",
      })
    } finally {
      setUpdating(false)
    }
  }

  const formatVariantAttributes = (attributes: Record<string, string> | string): string => {
    let attrs: Record<string, string> = {}
    if (typeof attributes === 'string') {
      try {
        attrs = JSON.parse(attributes)
      } catch {
        return attributes
      }
    } else {
      attrs = attributes
    }
    
    return Object.entries(attrs)
      .map(([key, value]) => `${key}: ${value}`)
      .join(", ")
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading inventory...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
          Inventory Management
        </h1>
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          Track and manage product inventory and stock levels
        </p>
      </div>

      <div className="grid grid-cols-1 gap-6">
        {products.map((product) => (
          <div key={product.id} className="bg-white rounded-lg border border-border p-6">
            <div className="flex gap-6">
              {/* Product Image */}
              <div className="flex-shrink-0">
                {product.imageUrl ? (
                  <div className="w-24 h-24 relative rounded-md overflow-hidden bg-gray-100">
                    <Image
                      src={product.imageUrl}
                      alt={product.name}
                      fill
                      className="object-cover"
                    />
                  </div>
                ) : (
                  <div className="w-24 h-24 bg-gray-100 rounded-md flex items-center justify-center">
                    <Package className="h-8 w-8 text-gray-400" />
                  </div>
                )}
              </div>

              {/* Product Info */}
              <div className="flex-1">
                <h3 className="text-lg font-semibold mb-1" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  {product.name}
                </h3>
                <p className="text-sm text-muted-foreground mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
                  {product.categoryName}
                </p>

                {product.hasVariants ? (
                  // Product with variants - show each variant with its stock
                  <div className="space-y-3">
                    {product.variants.map((variant) => (
                      <div key={variant.id} className="border-t pt-3 first:border-t-0 first:pt-0">
                        <div className="flex items-center justify-between">
                          <div className="flex-1">
                            <p className="text-sm font-medium mb-1" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                              {formatVariantAttributes(variant.attributes)}
                            </p>
                            <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                              Current Stock: <span className="font-medium text-foreground">{variant.variantInventory?.quantity || 0} {variant.variantInventory?.unit || "piece"}</span>
                            </p>
                          </div>
                          <Button
                            onClick={() => openUpdateDialog(product, variant)}
                            variant="outline"
                            size="sm"
                            style={{ fontFamily: '"Dream Avenue"', borderColor: '#3D0811', color: '#3D0811' }}
                          >
                            {variant.variantInventory ? "Update Stock" : "Set Stock"}
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  // Product without variants
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm text-muted-foreground mb-1" style={{ fontFamily: '"Dream Avenue"' }}>
                        Current Stock
                      </p>
                      <p className="text-lg font-semibold" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                        {product.productInventory?.quantity || 0} {product.productInventory?.unit || "piece"}
                      </p>
                    </div>
                    <Button
                      onClick={() => openUpdateDialog(product)}
                      variant="outline"
                      style={{ fontFamily: '"Dream Avenue"', borderColor: '#3D0811', color: '#3D0811' }}
                    >
                      {product.productInventory ? "Update Stock" : "Set Stock"}
                    </Button>
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Update Stock Dialog */}
      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent style={{ fontFamily: '"Dream Avenue"' }}>
          <DialogHeader>
            <DialogTitle style={{ color: '#3D0811' }}>
              {selectedVariant 
                ? `Update Stock - ${formatVariantAttributes(selectedVariant.attributes)}` 
                : `Update Stock - ${selectedProduct?.name}`}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="quantity" style={{ fontFamily: '"Dream Avenue"' }}>Quantity</Label>
              <Input
                id="quantity"
                type="number"
                min="0"
                step="0.01"
                value={stockQuantity}
                onChange={(e) => setStockQuantity(e.target.value)}
                style={{ fontFamily: '"Dream Avenue"' }}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsDialogOpen(false)}
              style={{ fontFamily: '"Dream Avenue"', borderColor: '#3D0811', color: '#3D0811' }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleUpdateStock}
              disabled={updating}
              style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
            >
              {updating ? "Updating..." : "Update Stock"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
