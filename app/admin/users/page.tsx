"use client"

import { useState, useEffect } from "react"
import { Search, Edit } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { api } from "@/lib/api-client"

interface User {
  id: number
  fullName: string
  email: string
  isActive: boolean
  roles: string[]
  createdAt: string
}

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState("")

  const [roles, setRoles] = useState<any[]>([])
  const [editingUser, setEditingUser] = useState<User | null>(null)
  const [showEditModal, setShowEditModal] = useState(false)
  const [saveLoading, setSaveLoading] = useState(false)
  
  // Edit form state
  const [editFormData, setEditFormData] = useState({
    fullName: "",
    email: "",
    isActive: false,
    roles: [] as string[]
  })

  useEffect(() => {
    loadUsers()
    loadRoles()
  }, [])

  const loadRoles = async () => {
    try {
      const data: any = await api.admin.getRoles()
      setRoles(data)
    } catch (error) {
      console.error("Error loading roles:", error)
    }
  }

  const loadUsers = async () => {
    try {
      setLoading(true)
      const data = await api.admin.getUsers() as User[]
      setUsers(data)
    } catch (error) {
      console.error("Error loading users:", error)
    } finally {
      setLoading(false)
    }
  }

  const handleEditClick = (user: User) => {
    setEditingUser(user)
    setEditFormData({
      fullName: user.fullName,
      email: user.email,
      isActive: user.isActive,
      roles: [...user.roles]
    })
    setShowEditModal(true)
  }

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingUser) return

    try {
      setSaveLoading(true)
      await api.admin.updateUser(editingUser.id, editFormData)
      setShowEditModal(false)
      setEditingUser(null)
      loadUsers() // Reload list
    } catch (error) {
      console.error("Error updating user:", error)
      alert("Failed to update user")
    } finally {
      setSaveLoading(false)
    }
  }

  const toggleRole = (roleName: string) => {
    setEditFormData(prev => {
      const newRoles = prev.roles.includes(roleName)
        ? prev.roles.filter(r => r !== roleName)
        : [...prev.roles, roleName]
      return { ...prev, roles: newRoles }
    })
  }

  const filteredUsers = users.filter(
    (user) =>
      user.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.email.toLowerCase().includes(searchTerm.toLowerCase())
  )

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-lg" style={{ fontFamily: '"Dream Avenue"' }}>Loading users...</div>
      </div>
    )
  }

  return (
    <div className="space-y-6 relative">
      {/* Edit Modal */}
      {showEditModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-white rounded-lg p-6 w-full max-w-md shadow-xl" style={{ maxHeight: '90vh', overflowY: 'auto' }}>
            <h2 className="text-2xl font-semibold mb-4" style={{ fontFamily: '"Dream Avenue"' }}>Edit User</h2>
            <form onSubmit={handleEditSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">Full Name</label>
                <Input 
                  value={editFormData.fullName}
                  onChange={e => setEditFormData({...editFormData, fullName: e.target.value})}
                  required
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium mb-1">Email</label>
                <Input 
                  type="email"
                  value={editFormData.email}
                  onChange={e => setEditFormData({...editFormData, email: e.target.value})}
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium mb-1">Status</label>
                <div className="flex items-center gap-2">
                  <input 
                    type="checkbox"
                    id="isActive"
                    checked={editFormData.isActive}
                    onChange={e => setEditFormData({...editFormData, isActive: e.target.checked})}
                    className="h-4 w-4 rounded border-gray-300"
                  />
                  <label htmlFor="isActive">Active</label>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium mb-2">Roles</label>
                <div className="space-y-2 border p-3 rounded-md">
                  {roles.map(role => (
                    <div key={role.id} className="flex items-center gap-2">
                       <input 
                        type="checkbox"
                        id={`role-${role.id}`}
                        checked={editFormData.roles.includes(role.name)}
                        onChange={() => toggleRole(role.name)}
                        className="h-4 w-4 rounded border-gray-300"
                      />
                      <label htmlFor={`role-${role.id}`}>{role.name}</label>
                    </div>
                  ))}
                </div>
              </div>

              <div className="flex justify-end gap-2 pt-4">
                <Button type="button" variant="ghost" onClick={() => setShowEditModal(false)}>Cancel</Button>
                <Button type="submit" disabled={saveLoading} style={{ backgroundColor: '#3D0811' }}>
                  {saveLoading ? 'Saving...' : 'Save Changes'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div>
        <h1 className="text-3xl font-semibold mb-2" style={{ fontFamily: '"Dream Avenue"', color: '#3D0811' }}>
          Users & Roles
        </h1>
        <p className="text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
          Manage users and their roles
        </p>
      </div>

      <div className="bg-white rounded-lg p-4 border border-border">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search users by name or email..."
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
                  Email
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Roles
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Status
                </th>
                <th className="px-6 py-3 text-left text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Created
                </th>
                <th className="px-6 py-3 text-right text-sm font-medium text-white" style={{ fontFamily: '"Dream Avenue"' }}>
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filteredUsers.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                    No users found
                  </td>
                </tr>
              ) : (
                filteredUsers.map((user) => (
                  <tr key={user.id} className="hover:bg-muted/50">
                    <td className="px-6 py-4 font-medium" style={{ fontFamily: '"Dream Avenue"' }}>
                      {user.fullName}
                    </td>
                    <td className="px-6 py-4 text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {user.email}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap gap-1">
                        {user.roles.map((role) => (
                          <span
                            key={role}
                            className="px-2 py-1 rounded text-xs bg-blue-100 text-blue-800"
                            style={{ fontFamily: '"Dream Avenue"' }}
                          >
                            {role}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          user.isActive ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800"
                        }`}
                        style={{ fontFamily: '"Dream Avenue"' }}
                      >
                        {user.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-muted-foreground" style={{ fontFamily: '"Dream Avenue"' }}>
                      {new Date(user.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button variant="ghost" size="sm" onClick={() => handleEditClick(user)}>
                          <Edit className="h-4 w-4" />
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











