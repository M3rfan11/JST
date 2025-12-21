"use client"

import { useState, useEffect } from "react"
import { Plus, Edit, Trash2, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { api } from "@/lib/api-client"
import Link from "next/link"

interface PromoCode {
  id: number
  code: string
  discountType: string
  discountValue: number
  startDate: string
  endDate: string
  isActive: boolean
}

export default function PromoCodesPage() {
  const [promoCodes, setPromoCodes] = useState<PromoCode[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState("")

  useEffect(() => {
    loadPromoCodes()
  }, [])

  const loadPromoCodes = async () => {
    try {
      setLoading(true)
      const data = await api.promoCodes.getAll()
      setPromoCodes(data)
    } catch (error) {
      console.error("Error loading promo codes:", error)
    } finally {
      setLoading(false)
    }
  }

  const filteredPromoCodes = promoCodes.filter((promo) =>
    promo.code.toLowerCase().includes(searchTerm.toLowerCase())
  )

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading promo codes...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
            Promo Codes
          </h1>
          <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
            Manage discount codes and promotions
          </p>
        </div>
        <Button
          asChild
          style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
        >
          <Link href="/admin/promo-codes/new">
            <Plus className="h-4 w-4 mr-2" />
            Add Promo Code
          </Link>
        </Button>
      </div>

      <div className="bg-white rounded-lg p-4 border border-border">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search promo codes..."
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
                  Code
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Discount
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Valid From
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Valid Until
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
              {filteredPromoCodes.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    No promo codes found
                  </td>
                </tr>
              ) : (
                filteredPromoCodes.map((promo) => (
                  <tr key={promo.id} className="hover:bg-muted/50">
                    <td className="px-6 py-4 font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                      {promo.code}
                    </td>
                    <td className="px-6 py-4" style={{ fontFamily: '"Dream Avenue"' }}>
                      {promo.discountType === "Percentage"
                        ? `${promo.discountValue}%`
                        : `$${promo.discountValue.toFixed(2)}`}
                    </td>
                    <td className="px-6 py-4 text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {new Date(promo.startDate).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {promo.endDate ? new Date(promo.endDate).toLocaleDateString() : "No end date"}
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          promo.isActive ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800"
                        }`}
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {promo.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button variant="ghost" size="sm" asChild>
                          <Link href={`/admin/promo-codes/${promo.id}/edit`}>
                            <Edit className="h-4 w-4" />
                          </Link>
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            // TODO: Implement delete
                            console.log("Delete promo code", promo.id)
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


