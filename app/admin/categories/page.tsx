"use client"

import { useState, useEffect } from "react"
import { Plus, Edit, Trash2, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { api } from "@/lib/api-client"
import { useToast } from "@/hooks/use-toast"
import Link from "next/link"

interface Category {
  id: number
  name: string
  description: string
  isActive: boolean
}

export default function CategoriesPage() {
  const { toast } = useToast()
  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState("")

  useEffect(() => {
    loadCategories()
  }, [])

  const loadCategories = async () => {
    try {
      setLoading(true)
      const data = await api.categories.getAll()
      // Handle different response formats
      const categoriesList = Array.isArray(data) ? data : (data?.value || data?.data || [])
      setCategories(categoriesList)
    } catch (error) {
      console.error("Error loading categories:", error)
      toast({
        title: "Error",
        description: "Failed to load categories.",
        variant: "destructive",
      })
    } finally {
      setLoading(false)
    }
  }

  const filteredCategories = categories.filter((category) =>
    category.name.toLowerCase().includes(searchTerm.toLowerCase())
  )

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading categories...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            Categories
          </h1>
          <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
            Manage product categories
          </p>
        </div>
        <Button
          asChild
          style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
        >
          <Link href="/admin/categories/new">
            <Plus className="h-4 w-4 mr-2" />
            Add Category
          </Link>
        </Button>
      </div>

      <div className="bg-white rounded-lg p-4 border border-border">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search categories..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10"
            style={{ fontFamily: '"Dream Avenue"' }}
          />
        </div>
      </div>

      <div className="bg-white rounded-lg border border-border overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead style={{ backgroundColor: '#3D0811' }}>
              <tr>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Name
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Description
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
              {filteredCategories.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-6 py-8 text-center text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    No categories found
                  </td>
                </tr>
              ) : (
                filteredCategories.map((category) => (
                  <tr key={category.id} className="hover:bg-muted/50">
                    <td className="px-6 py-4 font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                      {category.name}
                    </td>
                    <td className="px-6 py-4 text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {category.description || "—"}
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          category.isActive ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800"
                        }`}
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {category.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button variant="ghost" size="sm" asChild>
                          <Link href={`/admin/categories/${category.id}/edit`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={async () => {
                            if (confirm(`Are you sure you want to delete "${category.name}"?`)) {
                              try {
                                await api.categories.delete(category.id)
                                toast({
                                  title: "Success",
                                  description: "Category deleted successfully.",
                                })
                                loadCategories()
                              } catch (error: any) {
                                toast({
                                  title: "Error",
                                  description: error.message || "Failed to delete category.",
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

