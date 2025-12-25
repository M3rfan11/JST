"use client"

import { useState, useEffect } from "react"
import { useRouter, useParams } from "next/navigation"
import { X, AlertCircle } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { api } from "@/lib/api-client"
import { useToast } from "@/hooks/use-toast"
import Link from "next/link"

interface Customer {
  id: number
  email: string
  fullName: string
  phone?: string
  isRegistered: boolean
  orderCount: number
  lastOrderDate?: string
}

interface PromoCodeResponse {
  id: number
  code: string
  description?: string
  discountType: string
  discountValue: number
  startDate: string
  endDate?: string
  usageLimit?: number
  usedCount: number
  usageLimitPerUser?: number
  minimumOrderAmount?: number
  maximumDiscountAmount?: number
  isActive: boolean
  userIds: number[]
  productIds: number[]
}

export default function EditPromoCodePage() {
  const router = useRouter()
  const params = useParams()
  const promoCodeId = params?.id ? parseInt(params.id as string) : null
  const { toast } = useToast()
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [customers, setCustomers] = useState<Customer[]>([])
  const [selectedCustomerIds, setSelectedCustomerIds] = useState<number[]>([])
  const [additionalEmails, setAdditionalEmails] = useState("")

  const [formData, setFormData] = useState({
    code: "",
    description: "",
    discountType: "Percentage",
    discountValue: "",
    usageLimit: "",
    usageLimitPerUser: "",
    minimumOrderAmount: "",
    maximumDiscountAmount: "",
    startDate: "",
    endDate: "",
    isActive: true,
  })

  useEffect(() => {
    if (promoCodeId) {
      loadPromoCode()
      loadCustomers()
    }
  }, [promoCodeId])

  const loadPromoCode = async () => {
    if (!promoCodeId) return

    try {
      setLoading(true)
      const data = await api.promoCodes.getById(promoCodeId) as any
      
      // Handle different response formats
      const promoCode: PromoCodeResponse = data?.value || data?.data || data

      if (promoCode) {
        // Format dates for input fields (YYYY-MM-DD)
        const startDate = new Date(promoCode.startDate).toISOString().split('T')[0]
        const endDate = promoCode.endDate ? new Date(promoCode.endDate).toISOString().split('T')[0] : ""

        setFormData({
          code: promoCode.code,
          description: promoCode.description || "",
          discountType: promoCode.discountType,
          discountValue: promoCode.discountValue.toString(),
          usageLimit: promoCode.usageLimit?.toString() || "",
          usageLimitPerUser: promoCode.usageLimitPerUser?.toString() || "",
          minimumOrderAmount: promoCode.minimumOrderAmount?.toString() || "",
          maximumDiscountAmount: promoCode.maximumDiscountAmount?.toString() || "",
          startDate: startDate,
          endDate: endDate,
          isActive: promoCode.isActive,
        })

        setSelectedCustomerIds(promoCode.userIds || [])
      }
    } catch (error) {
      console.error("Error loading promo code:", error)
      toast({
        title: "Error",
        description: "Failed to load promo code",
        variant: "destructive",
      })
      router.push("/admin/promo-codes")
    } finally {
      setLoading(false)
    }
  }

  const loadCustomers = async () => {
    try {
      const data = await api.promoCodes.getCustomers() as any
      let customersList: Customer[] = []
      if (Array.isArray(data)) {
        customersList = data
      } else if (data && Array.isArray(data.value)) {
        customersList = data.value
      } else if (data && data.data && Array.isArray(data.data)) {
        customersList = data.data
      }
      setCustomers(customersList)
    } catch (error) {
      console.error("Error loading customers:", error)
    }
  }

  const generateCode = () => {
    const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    let code = ""
    for (let i = 0; i < 10; i++) {
      code += chars.charAt(Math.floor(Math.random() * chars.length))
    }
    setFormData({ ...formData, code })
  }

  const handleCustomerToggle = (customerId: number) => {
    setSelectedCustomerIds((prev) =>
      prev.includes(customerId)
        ? prev.filter((id) => id !== customerId)
        : [...prev, customerId]
    )
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!formData.code.trim()) {
      toast({
        title: "Error",
        description: "Promo code is required",
        variant: "destructive",
      })
      return
    }

    if (!formData.discountValue) {
      toast({
        title: "Error",
        description: "Discount value is required",
        variant: "destructive",
      })
      return
    }

    if (!formData.startDate) {
      toast({
        title: "Error",
        description: "Start date is required",
        variant: "destructive",
      })
      return
    }

    if (!promoCodeId) return

    try {
      setSaving(true)

      const requestData = {
        code: formData.code.trim().toUpperCase(),
        description: formData.description || null,
        discountType: formData.discountType,
        discountValue: parseFloat(formData.discountValue),
        startDate: new Date(formData.startDate).toISOString(),
        endDate: formData.endDate ? new Date(formData.endDate).toISOString() : null,
        usageLimit: formData.usageLimit ? parseInt(formData.usageLimit) : null,
        usageLimitPerUser: formData.usageLimitPerUser ? parseInt(formData.usageLimitPerUser) : null,
        minimumOrderAmount: formData.minimumOrderAmount ? parseFloat(formData.minimumOrderAmount) : null,
        maximumDiscountAmount: formData.maximumDiscountAmount ? parseFloat(formData.maximumDiscountAmount) : null,
        isActive: formData.isActive,
        userIds: selectedCustomerIds.length > 0 ? selectedCustomerIds : null,
        emailAddresses: additionalEmails
          ? additionalEmails.split(",").map((e) => e.trim()).filter((e) => e.length > 0)
          : null,
        sendEmailNotification: false, // Don't send emails on update
      }

      await api.promoCodes.update(promoCodeId, requestData)

      toast({
        title: "Success",
        description: "Promo code updated successfully",
      })

      router.push("/admin/promo-codes")
    } catch (error: any) {
      console.error("Error updating promo code:", error)
      toast({
        title: "Error",
        description: error.message || "Failed to update promo code",
        variant: "destructive",
      })
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading promo code...</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
              Edit Promo Code
            </h1>
            <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
              Update promotional code details
            </p>
          </div>
          <Button
            variant="ghost"
            size="icon"
            asChild
          >
            <Link href="/admin/promo-codes">
              <X className="h-5 w-5" />
            </Link>
          </Button>
        </div>

        {/* Form Card */}
        <div className="bg-white rounded-lg shadow-sm border border-border">
          <form onSubmit={handleSubmit} className="p-6 space-y-6">
            {/* Promo Code Details */}
            <div className="space-y-4">
              <h2 className="text-xl font-semibold" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                Promo Code Details
              </h2>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Promo Code */}
                <div className="space-y-2">
                  <Label htmlFor="code" style={{ fontFamily: '"Dream Avenue"' }}>
                    Promo Code <span className="text-red-500">*</span>
                  </Label>
                  <div className="flex gap-2">
                    <Input
                      id="code"
                      value={formData.code}
                      onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
                      placeholder="SUMMER2024"
                      style={{ fontFamily: '"Dream Avenue"' }}
                      required
                    />
                    <Button
                      type="button"
                      variant="outline"
                      onClick={generateCode}
                      style={{ fontFamily: '"Dream Avenue"', borderColor: '#3D0811', color: '#3D0811' }}
                    >
                      Generate
                    </Button>
                  </div>
                </div>

                {/* Discount Type */}
                <div className="space-y-2">
                  <Label htmlFor="discountType" style={{ fontFamily: '"Dream Avenue"' }}>
                    Discount Type <span className="text-red-500">*</span>
                  </Label>
                  <select
                    id="discountType"
                    value={formData.discountType}
                    onChange={(e) => setFormData({ ...formData, discountType: e.target.value })}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                    style={{ fontFamily: '"Dream Avenue"' }}
                    required
                  >
                    <option value="Percentage">Percentage (%)</option>
                    <option value="Fixed">Fixed Amount</option>
                  </select>
                </div>

                {/* Discount Value */}
                <div className="space-y-2">
                  <Label htmlFor="discountValue" style={{ fontFamily: '"Dream Avenue"' }}>
                    Discount Value <span className="text-red-500">*</span>
                  </Label>
                  <Input
                    id="discountValue"
                    type="number"
                    step="0.01"
                    min="0"
                    max={formData.discountType === "Percentage" ? "100" : undefined}
                    value={formData.discountValue}
                    onChange={(e) => setFormData({ ...formData, discountValue: e.target.value })}
                    placeholder={formData.discountType === "Percentage" ? "10" : "50.00"}
                    style={{ fontFamily: '"Dream Avenue"' }}
                    required
                  />
                </div>

                {/* Usage Limit (Total) */}
                <div className="space-y-2">
                  <Label htmlFor="usageLimit" style={{ fontFamily: '"Dream Avenue"' }}>
                    Usage Limit (Total)
                  </Label>
                  <Input
                    id="usageLimit"
                    type="number"
                    min="1"
                    value={formData.usageLimit}
                    onChange={(e) => setFormData({ ...formData, usageLimit: e.target.value })}
                    placeholder="Leave empty for unlimited"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                {/* Usage Limit Per User */}
                <div className="space-y-2">
                  <Label htmlFor="usageLimitPerUser" style={{ fontFamily: '"Dream Avenue"' }}>
                    Usage Limit Per User
                  </Label>
                  <Input
                    id="usageLimitPerUser"
                    type="number"
                    min="1"
                    value={formData.usageLimitPerUser}
                    onChange={(e) => setFormData({ ...formData, usageLimitPerUser: e.target.value })}
                    placeholder="Leave empty for unlimited"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                {/* Minimum Order Amount */}
                <div className="space-y-2">
                  <Label htmlFor="minimumOrderAmount" style={{ fontFamily: '"Dream Avenue"' }}>
                    Minimum Order Amount
                  </Label>
                  <Input
                    id="minimumOrderAmount"
                    type="number"
                    step="0.01"
                    min="0"
                    value={formData.minimumOrderAmount}
                    onChange={(e) => setFormData({ ...formData, minimumOrderAmount: e.target.value })}
                    placeholder="Leave empty for no minimum"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                {/* Maximum Discount Amount */}
                <div className="space-y-2">
                  <Label htmlFor="maximumDiscountAmount" style={{ fontFamily: '"Dream Avenue"' }}>
                    Maximum Discount Amount
                  </Label>
                  <Input
                    id="maximumDiscountAmount"
                    type="number"
                    step="0.01"
                    min="0"
                    value={formData.maximumDiscountAmount}
                    onChange={(e) => setFormData({ ...formData, maximumDiscountAmount: e.target.value })}
                    placeholder="Leave empty for no maximum"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                {/* Start Date */}
                <div className="space-y-2">
                  <Label htmlFor="startDate" style={{ fontFamily: '"Dream Avenue"' }}>
                    Start Date <span className="text-red-500">*</span>
                  </Label>
                  <Input
                    id="startDate"
                    type="date"
                    value={formData.startDate}
                    onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                    style={{ fontFamily: '"Dream Avenue"' }}
                    required
                  />
                </div>

                {/* End Date */}
                <div className="space-y-2">
                  <Label htmlFor="endDate" style={{ fontFamily: '"Dream Avenue"' }}>
                    End Date
                  </Label>
                  <Input
                    id="endDate"
                    type="date"
                    value={formData.endDate}
                    onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                    min={formData.startDate}
                    style={{ fontFamily: '"Dream Avenue"' }}
                  />
                </div>

                {/* Is Active */}
                <div className="space-y-2">
                  <Label htmlFor="isActive" style={{ fontFamily: '"Dream Avenue"' }}>
                    Status
                  </Label>
                  <select
                    id="isActive"
                    value={formData.isActive ? "true" : "false"}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.value === "true" })}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                    style={{ fontFamily: '"Dream Avenue"' }}
                  >
                    <option value="true">Active</option>
                    <option value="false">Inactive</option>
                  </select>
                </div>
              </div>

              {/* Description */}
              <div className="space-y-2">
                <Label htmlFor="description" style={{ fontFamily: '"Dream Avenue"' }}>
                  Description
                </Label>
                <textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Optional description for this promo code"
                  rows={3}
                  className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  style={{ fontFamily: '"Dream Avenue"' }}
                />
              </div>
            </div>

            {/* Select Customers */}
            <div className="space-y-4 border-t pt-6">
              <div>
                <h2 className="text-xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  Select Customers
                </h2>
                <p className="text-sm text-muted-foreground mb-3" style={{ fontFamily: '"Dream Avenue"' }}>
                  Select customers to send promo codes to. All customers can receive promo codes, but they must be logged in to use them.
                </p>
                <div className="flex items-start gap-2 text-sm text-amber-600 bg-amber-50 p-3 rounded-md">
                  <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
                  <span style={{ fontFamily: '"Dream Avenue"' }}>
                    Leave empty to allow all registered customers.
                  </span>
                </div>
              </div>

              <div className="border rounded-md p-4 max-h-96 overflow-y-auto">
                {customers.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-4" style={{ fontFamily: '"Dream Avenue"' }}>
                    No customers found
                  </p>
                ) : (
                  <div className="space-y-3">
                    {customers.map((customer) => (
                      <div key={customer.id} className="flex items-center space-x-3">
                        <input
                          type="checkbox"
                          id={`customer-${customer.id}`}
                          checked={selectedCustomerIds.includes(customer.id)}
                          onChange={() => handleCustomerToggle(customer.id)}
                          className="h-4 w-4 rounded border-gray-300 text-[#3D0811] focus:ring-[#3D0811]"
                        />
                        <Label
                          htmlFor={`customer-${customer.id}`}
                          className="cursor-pointer flex-1"
                          style={{ fontFamily: '"Dream Avenue"' }}
                        >
                          <div className="flex items-center justify-between">
                            <div>
                              <span className="font-medium">{customer.fullName}</span>
                              <span className="text-muted-foreground ml-2">({customer.email})</span>
                            </div>
                            <span className="text-sm text-muted-foreground">
                              {customer.orderCount > 0
                                ? `${customer.orderCount} ${customer.orderCount === 1 ? "order" : "orders"}`
                                : "Registered"}
                            </span>
                          </div>
                        </Label>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {/* Send to Additional Email Addresses */}
            <div className="space-y-4 border-t pt-6">
              <div>
                <h2 className="text-xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
                  Send to Additional Email Addresses (Optional)
                </h2>
                <p className="text-sm text-muted-foreground mb-3" style={{ fontFamily: '"Dream Avenue"' }}>
                  Enter additional email addresses to send promo code notifications. All recipients will receive the code but must be logged in to use it.
                </p>
              </div>
              <Input
                value={additionalEmails}
                onChange={(e) => setAdditionalEmails(e.target.value)}
                placeholder="email1@example.com, email2@example.com"
                style={{ fontFamily: '"Dream Avenue"' }}
              />
            </div>

            {/* Action Buttons */}
            <div className="flex items-center justify-end gap-4 pt-4 border-t">
              <Button
                type="button"
                variant="outline"
                onClick={() => router.push("/admin/promo-codes")}
                style={{ fontFamily: '"Dream Avenue"' }}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={saving}
                style={{
                  fontFamily: '"Dream Avenue"',
                  backgroundColor: '#3D0811',
                  color: 'white',
                }}
                className="hover:opacity-90"
              >
                {saving ? "Updating..." : "Update Promo Code"}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}




