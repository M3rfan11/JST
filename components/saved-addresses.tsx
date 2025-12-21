"use client"

import type React from "react"

import { useState, useEffect } from "react"
import { Button } from "./ui/button"
import { Input } from "./ui/input"
import { Label } from "./ui/label"
import { useToast } from "@/hooks/use-toast"
import { Plus, Edit, Trash2 } from "lucide-react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "./ui/dialog"

interface Address {
  id: string
  label: string
  firstName: string
  lastName: string
  address: string
  apartment: string
  city: string
  state: string
  zipCode: string
  country: string
  isDefault: boolean
}

export function SavedAddresses() {
  const { toast } = useToast()
  const [addresses, setAddresses] = useState<Address[]>([])
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [editingAddress, setEditingAddress] = useState<Address | null>(null)
  const [formData, setFormData] = useState({
    label: "",
    firstName: "",
    lastName: "",
    address: "",
    apartment: "",
    city: "",
    state: "",
    zipCode: "",
    country: "",
    isDefault: false,
  })

  useEffect(() => {
    const saved = localStorage.getItem("addresses")
    if (saved) {
      setAddresses(JSON.parse(saved))
    }
  }, [])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.type === "checkbox" ? e.target.checked : e.target.value
    setFormData({ ...formData, [e.target.name]: value })
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    let updatedAddresses: Address[]

    if (editingAddress) {
      // Update existing address
      updatedAddresses = addresses.map((addr) => (addr.id === editingAddress.id ? { ...formData, id: addr.id } : addr))
    } else {
      // Add new address
      const newAddress: Address = {
        ...formData,
        id: Math.random().toString(36).substr(2, 9),
      }
      updatedAddresses = [...addresses, newAddress]
    }

    // If this address is set as default, remove default from others
    if (formData.isDefault) {
      updatedAddresses = updatedAddresses.map((addr) =>
        addr.id === (editingAddress?.id || updatedAddresses[updatedAddresses.length - 1].id)
          ? addr
          : { ...addr, isDefault: false },
      )
    }

    setAddresses(updatedAddresses)
    localStorage.setItem("addresses", JSON.stringify(updatedAddresses))

    toast({
      title: editingAddress ? "Address updated" : "Address added",
      description: "Your changes have been saved successfully.",
    })

    setIsDialogOpen(false)
    setEditingAddress(null)
    setFormData({
      label: "",
      firstName: "",
      lastName: "",
      address: "",
      apartment: "",
      city: "",
      state: "",
      zipCode: "",
      country: "",
      isDefault: false,
    })
  }

  const handleEdit = (address: Address) => {
    setEditingAddress(address)
    setFormData(address)
    setIsDialogOpen(true)
  }

  const handleDelete = (id: string) => {
    const updatedAddresses = addresses.filter((addr) => addr.id !== id)
    setAddresses(updatedAddresses)
    localStorage.setItem("addresses", JSON.stringify(updatedAddresses))

    toast({
      title: "Address deleted",
      description: "The address has been removed.",
    })
  }

  return (
    <div className="max-w-6xl">
      <div className="flex items-center justify-between mb-8">
        <h2 className="font-serif text-2xl sm:text-3xl font-semibold" style={{ fontFamily: '"Dream Avenue"' }}>Saved Addresses</h2>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button
              size="lg"
              className="h-12"
              onClick={() => {
                setEditingAddress(null)
                setFormData({
                  label: "",
                  firstName: "",
                  lastName: "",
                  address: "",
                  apartment: "",
                  city: "",
                  state: "",
                  zipCode: "",
                  country: "",
                  isDefault: false,
                })
              }}
              style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}
            >
              <Plus className="h-4 w-4 mr-2" />
              Add Address
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
            <DialogHeader className="mb-6">
              <DialogTitle className="font-serif text-2xl sm:text-3xl" style={{ fontFamily: '"Dream Avenue"' }}>
                {editingAddress ? "Edit Address" : "Add New Address"}
              </DialogTitle>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="space-y-5">
              <div className="space-y-2">
                <Label htmlFor="label" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                  Address Label
                </Label>
                <Input
                  id="label"
                  name="label"
                  value={formData.label}
                  onChange={handleChange}
                  placeholder="e.g., Home, Office"
                  required
                  className="h-12"
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
                <div className="space-y-2">
                  <Label htmlFor="firstName" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                    First Name
                  </Label>
                  <Input
                    id="firstName"
                    name="firstName"
                    value={formData.firstName}
                    onChange={handleChange}
                    required
                    className="h-12"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="lastName" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                    Last Name
                  </Label>
                  <Input
                    id="lastName"
                    name="lastName"
                    value={formData.lastName}
                    onChange={handleChange}
                    required
                    className="h-12"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="address" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                  Address
                </Label>
                <Input
                  id="address"
                  name="address"
                  value={formData.address}
                  onChange={handleChange}
                  required
                  className="h-12"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="apartment" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                  Apartment, suite, etc. (optional)
                </Label>
                <Input
                  id="apartment"
                  name="apartment"
                  value={formData.apartment}
                  onChange={handleChange}
                  className="h-12"
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
                <div className="space-y-2">
                  <Label htmlFor="city" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                    City
                  </Label>
                  <Input
                    id="city"
                    name="city"
                    value={formData.city}
                    onChange={handleChange}
                    required
                    className="h-12"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="state" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                    State / Province
                  </Label>
                  <Input
                    id="state"
                    name="state"
                    value={formData.state}
                    onChange={handleChange}
                    required
                    className="h-12"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
                <div className="space-y-2">
                  <Label htmlFor="country" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                    Country
                  </Label>
                  <Input
                    id="country"
                    name="country"
                    value={formData.country}
                    onChange={handleChange}
                    required
                    className="h-12"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="zipCode" className="text-base font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                    ZIP / Postal Code
                  </Label>
                  <Input
                    id="zipCode"
                    name="zipCode"
                    value={formData.zipCode}
                    onChange={handleChange}
                    required
                    className="h-12"
                  />
                </div>
              </div>

              <div className="flex items-center gap-3 pt-2">
                <input
                  type="checkbox"
                  id="isDefault"
                  name="isDefault"
                  checked={formData.isDefault}
                  onChange={handleChange}
                  className="h-5 w-5"
                />
                <Label htmlFor="isDefault" className="cursor-pointer text-base" style={{ fontFamily: '"Dream Avenue"' }}>
                  Set as default address
                </Label>
              </div>

              <div className="flex flex-col sm:flex-row gap-3 pt-6 border-t border-border">
                <Button type="submit" size="lg" className="flex-1 h-12 text-base" style={{ backgroundColor: '#3D0811', color: 'rgba(255, 255, 255, 1)', fontFamily: '"Dream Avenue"' }}>
                  {editingAddress ? "Update Address" : "Add Address"}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="lg"
                  className="h-12 text-base bg-transparent"
                  onClick={() => setIsDialogOpen(false)}
                  style={{ borderColor: '#3D0811', color: '#3D0811', fontFamily: '"Dream Avenue"' }}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {addresses.length === 0 ? (
        <div className="text-center py-12 border border-border">
          <p className="text-muted-foreground mb-4" style={{ fontFamily: '"Dream Avenue"' }}>No saved addresses yet</p>
          <p className="text-sm text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>Add an address to make checkout faster next time</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {addresses.map((address) => (
            <div key={address.id} className="border border-border p-6 relative">
              {address.isDefault && (
                <span className="absolute top-4 right-4 text-xs font-medium bg-foreground text-background px-2 py-1" style={{ fontFamily: '"Dream Avenue"' }}>
                  DEFAULT
                </span>
              )}
              <h3 className="font-medium mb-3" style={{ fontFamily: '"Dream Avenue"' }}>{address.label}</h3>
              <div className="text-sm text-muted-foreground space-y-1 mb-4" style={{ fontFamily: '"Dream Avenue"' }}>
                <p>
                  {address.firstName} {address.lastName}
                </p>
                <p>{address.address}</p>
                {address.apartment && <p>{address.apartment}</p>}
                <p>
                  {address.city}, {address.state} {address.zipCode}
                </p>
                <p>{address.country}</p>
              </div>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" onClick={() => handleEdit(address)} style={{ borderColor: '#3D0811', color: '#3D0811', fontFamily: '"Dream Avenue"' }}>
                  <Edit className="h-4 w-4 mr-1" />
                  Edit
                </Button>
                <Button variant="ghost" size="sm" onClick={() => handleDelete(address.id)} style={{ fontFamily: '"Dream Avenue"' }}>
                  <Trash2 className="h-4 w-4 mr-1" />
                  Delete
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
