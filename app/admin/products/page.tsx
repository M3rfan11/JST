"use client"

import { useState, useEffect } from "react"
import { Plus, Edit, Trash2, Search, Eye, EyeOff } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { api } from "@/lib/api-client"
import { useToast } from "@/hooks/use-toast"
import Link from "next/link"

interface Product {
  id: number
  name: string
  sku: string
  price: number
  isActive: boolean
  stockQuantity?: number
}

export default function ProductsPage() {
  const { toast } = useToast()
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState("")
  const [showInactive, setShowInactive] = useState(false)

  useEffect(() => {
    loadProducts()
  }, [])

  const loadProducts = async () => {
    try {
      setLoading(true)
      const response = await api.products.getAll() as any
      // Handle different response formats
      let productsList: any[] = []
      if (Array.isArray(response)) {
        productsList = response
      } else if (response && Array.isArray(response.value)) {
        productsList = response.value
      } else if (response && response.data && Array.isArray(response.data)) {
        productsList = response.data
      }
      
      // Map to Product interface - show all products regardless of status
      const mappedProducts: Product[] = productsList.map((p: any) => ({
        id: p.id,
        name: p.name || '',
        sku: p.sku || p.sKU || '',
        price: p.price || 0,
        // Handle different status formats: isActive boolean, status number (0=Draft, 1=Active), or status string
        isActive: p.isActive !== undefined 
          ? p.isActive 
          : (p.status === 1 || p.status === 'Active' || p.status === '1'),
        stockQuantity: p.totalQuantity || p.stockQuantity || p.quantity,
      }))
      
      setProducts(mappedProducts)
    } catch (error: any) {
      console.error("Error loading products:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to load products. Please check your connection.",
        variant: "destructive",
      })
      setProducts([])
    } finally {
      setLoading(false)
    }
  }

  const filteredProducts = products.filter(
    (product) => {
      // Filter by search term
      const matchesSearch = 
        product.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        product.sku.toLowerCase().includes(searchTerm.toLowerCase())
      
      // Filter by active status - by default, only show active products
      const matchesStatus = showInactive ? true : product.isActive
      
      return matchesSearch && matchesStatus
    }
  )

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading products...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            Products
          </h1>
          <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
            Manage your product catalog
          </p>
        </div>
        <Button
          asChild
          style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
        >
          <Link href="/admin/products/new">
            <Plus className="h-4 w-4 mr-2" />
            Add Product
          </Link>
        </Button>
      </div>

      {/* Search and Filters */}
      <div className="bg-white rounded-lg p-4 border border-border">
        <div className="flex items-center gap-4">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search products by name or SKU..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10"
              style={{ fontFamily: '"Dream Avenue"' }}
            />
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowInactive(!showInactive)}
            style={{ fontFamily: '"Dream Avenue"', borderColor: '#3D0811', color: '#3D0811' }}
          >
            {showInactive ? (
              <>
                <EyeOff className="h-4 w-4 mr-2" />
                Hide Inactive
              </>
            ) : (
              <>
                <Eye className="h-4 w-4 mr-2" />
                Show Inactive
              </>
            )}
          </Button>
        </div>
      </div>

      {/* Products Table */}
      <div className="bg-white rounded-lg border border-border overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead style={{ backgroundColor: '#3D0811' }}>
              <tr>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Name
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  SKU
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Price
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Stock
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Status
                </th>
                <th className="px-6 py-3 text-right text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filteredProducts.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    No products found
                  </td>
                </tr>
              ) : (
                filteredProducts.map((product) => (
                  <tr key={product.id} className="hover:bg-muted/50">
                    <td className="px-6 py-4" style={{ fontFamily: '"Dream Avenue"' }}>
                      {product.name}
                    </td>
                    <td className="px-6 py-4 text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {product.sku}
                    </td>
                    <td className="px-6 py-4" style={{ fontFamily: '"Dream Avenue"' }}>
                      {product.price.toFixed(2)} EGP
                    </td>
                    <td className="px-6 py-4" style={{ fontFamily: '"Dream Avenue"' }}>
                      {product.stockQuantity ?? "N/A"}
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          product.isActive ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800"
                        }`}
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {product.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          asChild
                        >
                          <Link href={`/admin/products/${product.id}/edit`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={async () => {
                            if (confirm(`Are you sure you want to delete "${product.name}"?`)) {
                              try {
                                await api.products.delete(product.id.toString())
                                toast({
                                  title: "Success",
                                  description: "Product deleted successfully.",
                                })
                                loadProducts()
                              } catch (error: any) {
                                toast({
                                  title: "Error",
                                  description: error.message || "Failed to delete product.",
                                  variant: "destructive",
                                })
                              }
                            }
                          }}
                        >
                          <Trash2 className="h-4 w-4 text-red-500" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

